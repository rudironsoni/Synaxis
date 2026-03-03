// <copyright file="ModelResolver.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.ControlPlane;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

    /// <summary>
    /// Resolves model identifiers to provider configurations.
    /// </summary>
    public class ModelResolver : IModelResolver
    {
        private readonly SynaxisConfiguration config;
        private readonly IProviderRegistry registry;
        private readonly IControlPlaneStore store;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelResolver"/> class.
        /// </summary>
        /// <param name="config">The Synaxis configuration.</param>
        /// <param name="registry">The provider registry.</param>
        /// <param name="store">The control plane store.</param>
        public ModelResolver(IOptions<SynaxisConfiguration> config, IProviderRegistry registry, IControlPlaneStore store)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(registry);
            ArgumentNullException.ThrowIfNull(store);
            this.config = config.Value;
            this.registry = registry;
            this.store = store;
        }

        /// <inheritdoc/>
        public ResolutionResult Resolve(string modelId, RequiredCapabilities? required = null)
        {
            // Fallback to config-based for sync callers
            return this.ResolveConfigOnly(modelId, required);
        }

        /// <inheritdoc/>
        public async Task<ResolutionResult> ResolveAsync(string modelId, EndpointKind kind, RequiredCapabilities? required = null, Guid? tenantId = null)
        {
            var globalResolution = await this.TryResolveGlobalModelAsync(modelId).ConfigureAwait(false);
            if (globalResolution != null)
            {
                return globalResolution;
            }

            var candidatesToTry = await this.GetCandidateModelsAsync(modelId, tenantId).ConfigureAwait(false);
            return this.ResolveCandidates(modelId, candidatesToTry, required);
        }

        private ResolutionResult ResolveConfigOnly(string modelId, RequiredCapabilities? required)
        {
            var candidatesToTry = new List<string>();
            if (this.config.Aliases.TryGetValue(modelId, out var alias))
            {
                candidatesToTry.AddRange(alias.Candidates);
            }
            else
            {
                candidatesToTry.Add(modelId);
            }

            return this.ResolveCandidates(modelId, candidatesToTry, required);
        }

        private ResolutionResult ResolveCandidates(string originalModelId, List<string> candidates, RequiredCapabilities? required)
        {
            CanonicalModelId? firstCanonicalId = null;

            foreach (var candidateId in candidates)
            {
                var candidateResolution = this.ResolveCandidate(originalModelId, candidateId, required);
                if (candidateResolution != null)
                {
                    return candidateResolution;
                }

                firstCanonicalId ??= this.GetCanonicalModelId(candidateId).CanonicalId;
            }

            // No match found
            var finalCanonicalId = firstCanonicalId ?? CanonicalModelId.Parse(originalModelId);
            return new ResolutionResult(originalModelId, finalCanonicalId, new List<ProviderConfig>());
        }

        private async Task<ResolutionResult?> TryResolveGlobalModelAsync(string modelId)
        {
            var global = await this.store.GetGlobalModelAsync(modelId).ConfigureAwait(false);
            if (global == null)
            {
                return null;
            }

            var canonicalId = CanonicalModelId.Parse(global.Id);
            var providers = this.BuildProviderConfigs(global.ProviderModels);
            return new ResolutionResult(modelId, canonicalId, providers);
        }

        private async Task<List<string>> GetCandidateModelsAsync(string modelId, Guid? tenantId)
        {
            if (tenantId.HasValue)
            {
                var candidates = await this.TryGetTenantCandidatesAsync(modelId, tenantId.Value).ConfigureAwait(false);
                if (candidates.Count > 0)
                {
                    return candidates;
                }
            }

            return this.GetConfigCandidates(modelId);
        }

        private async Task<List<string>> TryGetTenantCandidatesAsync(string modelId, Guid tenantId)
        {
            var candidatesToTry = new List<string>();
            var alias = await this.store.GetAliasAsync(tenantId, modelId).ConfigureAwait(false);
            var targetModel = alias?.TargetModel ?? modelId;

            var combo = await this.store.GetComboAsync(tenantId, targetModel).ConfigureAwait(false);
            if (combo != null)
            {
                var orderedModels = TryDeserializeModels(combo.OrderedModelsJson);
                if (orderedModels.Count > 0)
                {
                    candidatesToTry.AddRange(orderedModels);
                }
            }

            if (candidatesToTry.Count == 0 && alias != null)
            {
                candidatesToTry.Add(targetModel);
            }

            return candidatesToTry;
        }

        private static List<string> TryDeserializeModels(string orderedModelsJson)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(orderedModelsJson) ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        private List<string> GetConfigCandidates(string modelId)
        {
            if (this.config.Aliases.TryGetValue(modelId, out var configAlias))
            {
                return new List<string>(configAlias.Candidates);
            }

            return new List<string> { modelId };
        }

        private List<ProviderConfig> BuildProviderConfigs(IEnumerable<ProviderModel> providerModels)
        {
            var providers = new List<ProviderConfig>();
            foreach (var pm in providerModels)
            {
                if (!this.config.Providers.TryGetValue(pm.ProviderId, out var provCfg))
                {
                    continue;
                }

                if (!provCfg.Enabled)
                {
                    continue;
                }

                providers.Add(new ProviderConfig
                {
                    Enabled = provCfg.Enabled,
                    Key = pm.ProviderId,
                    AccountId = provCfg.AccountId,
                    ProjectId = provCfg.ProjectId,
                    AuthStoragePath = provCfg.AuthStoragePath,
                    Tier = provCfg.Tier,
                    Models = new List<string> { pm.ProviderSpecificId },
                    Type = provCfg.Type,
                    Endpoint = provCfg.Endpoint,
                    FallbackEndpoint = provCfg.FallbackEndpoint,
                });
            }

            return providers;
        }

        private ResolutionResult? ResolveCandidate(string originalModelId, string candidateId, RequiredCapabilities? required)
        {
            var (modelConfig, canonicalId) = this.GetCanonicalModelId(candidateId);
            if (!MeetsRequiredCapabilities(modelConfig, required))
            {
                return null;
            }

            var candidatePairs = this.registry.GetCandidates(canonicalId.ModelPath).ToList();
            if (candidatePairs.Count == 0 && modelConfig == null)
            {
                candidatePairs = this.registry.GetCandidates(candidateId).ToList();
                if (candidatePairs.Count > 0)
                {
                    canonicalId = new CanonicalModelId("unknown", candidateId);
                }
            }

            if (candidatePairs.Count == 0)
            {
                return null;
            }

            var providers = this.ResolveProviders(candidatePairs, canonicalId);
            if (providers.Count == 0)
            {
                return null;
            }

            if (string.Equals(canonicalId.Provider, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                canonicalId = new CanonicalModelId(providers[0].Key!, canonicalId.ModelPath);
            }

            return new ResolutionResult(originalModelId, canonicalId, providers);
        }

        private (CanonicalModelConfig? ModelConfig, CanonicalModelId CanonicalId) GetCanonicalModelId(string candidateId)
        {
            var modelConfig = this.config.CanonicalModels
                .FirstOrDefault(m => string.Equals(m.Id, candidateId, StringComparison.OrdinalIgnoreCase));

            var canonicalId = modelConfig != null
                ? new CanonicalModelId(modelConfig.Provider, modelConfig.ModelPath)
                : CanonicalModelId.Parse(candidateId);

            return (modelConfig, canonicalId);
        }

        private static bool MeetsRequiredCapabilities(CanonicalModelConfig? modelConfig, RequiredCapabilities? required)
        {
            if (required == null || modelConfig == null)
            {
                return true;
            }

            return (!required.Streaming || modelConfig.Streaming)
                   && (!required.Tools || modelConfig.Tools)
                   && (!required.Vision || modelConfig.Vision)
                   && (!required.StructuredOutput || modelConfig.StructuredOutput)
                   && (!required.LogProbs || modelConfig.LogProbs);
        }

        private List<ProviderConfig> ResolveProviders(IEnumerable<(string ServiceKey, int Tier)> candidatePairs, CanonicalModelId canonicalId)
        {
            var providers = candidatePairs
                .Select(p => this.TryMapProvider(p.ServiceKey))
                .Where(p => p != null)
                .Cast<ProviderConfig>()
                .ToList();

            if (!string.Equals(canonicalId.Provider, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                providers = providers
                    .Where(c => string.Equals(c.Key, canonicalId.Provider, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return providers;
        }

        private ProviderConfig? TryMapProvider(string serviceKey)
        {
            if (!this.config.Providers.TryGetValue(serviceKey, out var prov))
            {
                return null;
            }

            return new ProviderConfig
            {
                Enabled = prov.Enabled,
                Key = serviceKey,
                AccountId = prov.AccountId,
                ProjectId = prov.ProjectId,
                AuthStoragePath = prov.AuthStoragePath,
                Tier = prov.Tier,
                Models = prov.Models?.ToList() ?? new List<string>(),
                Type = prov.Type,
                Endpoint = prov.Endpoint,
                FallbackEndpoint = prov.FallbackEndpoint,
            };
        }
    }
}

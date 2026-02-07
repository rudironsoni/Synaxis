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
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (registry is null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "MA0051:Method is too long", Justification = "Complex resolution logic requires sequential steps")]
        public async Task<ResolutionResult> ResolveAsync(string modelId, EndpointKind kind, RequiredCapabilities? required = null, Guid? tenantId = null)
        {
            // Database-first: try to resolve a GlobalModel and its ProviderModels from the control plane DB
            var global = await this.store.GetGlobalModelAsync(modelId).ConfigureAwait(false);

            if (global != null)
            {
                var canonicalId = CanonicalModelId.Parse(global.Id);

                var providers = new List<ProviderConfig>();
                foreach (var pm in global.ProviderModels)
                {
                    if (!this.config.Providers.TryGetValue(pm.ProviderId, out var provCfg))
                    {
                        // provider config missing in static config -> skip
                        continue;
                    }

                    if (!provCfg.Enabled)
                    {
                        // provider disabled -> skip
                        continue;
                    }

                    // Create a shallow copy so we don't mutate the configured instance accidentally
                    var prov = new ProviderConfig
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
                    };

                    providers.Add(prov);
                }

                return new ResolutionResult(modelId, canonicalId, providers);
            }

            // If no GlobalModel found in DB, fall back to tenant-level aliases/combos and then to static config
            var candidatesToTry = new List<string>();
            bool foundInDb = false;

            if (tenantId.HasValue)
            {
                // 1. Check Model Aliases
                var alias = await this.store.GetAliasAsync(tenantId.Value, modelId).ConfigureAwait(false);
                string targetModel = alias?.TargetModel ?? modelId;

                // 2. Check Model Combos
                var combo = await this.store.GetComboAsync(tenantId.Value, targetModel).ConfigureAwait(false);

                if (combo != null)
                {
                    try
                    {
                        var models = JsonSerializer.Deserialize<List<string>>(combo.OrderedModelsJson);
                        if (models != null)
                        {
                            candidatesToTry.AddRange(models);
                            foundInDb = true;
                        }
                    }
                    catch
                    {
                        /* Ignore invalid JSON */
                    }
                }

                if (!foundInDb && alias != null)
                {
                    candidatesToTry.Add(targetModel);
                    foundInDb = true;
                }
            }

            if (!foundInDb)
            {
                // Fallback to Config
                if (this.config.Aliases.TryGetValue(modelId, out var configAlias))
                {
                    candidatesToTry.AddRange(configAlias.Candidates);
                }
                else
                {
                    candidatesToTry.Add(modelId);
                }
            }

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "MA0051:Method is too long", Justification = "Complex candidate resolution with multiple fallback paths")]
        private ResolutionResult ResolveCandidates(string originalModelId, List<string> candidates, RequiredCapabilities? required)
        {
            CanonicalModelId? firstCanonicalId = null;

            foreach (var candidateId in candidates)
            {
                // Step A: Canonical Lookup
                var modelConfig = this.config.CanonicalModels
                    .FirstOrDefault(m => string.Equals(m.Id, candidateId, StringComparison.OrdinalIgnoreCase));

                var canonicalId = modelConfig != null
                    ? new CanonicalModelId(modelConfig.Provider, modelConfig.ModelPath)
                    : CanonicalModelId.Parse(candidateId);

                firstCanonicalId ??= canonicalId;

                // Step B: Capability check (only possible when we have a canonical model config)
                if (required != null && modelConfig != null)
                {
                    bool meetsRequirements =
                        (!required.Streaming || modelConfig.Streaming) &&
                        (!required.Tools || modelConfig.Tools) &&
                        (!required.Vision || modelConfig.Vision) &&
                        (!required.StructuredOutput || modelConfig.StructuredOutput) &&
                        (!required.LogProbs || modelConfig.LogProbs);

                    if (!meetsRequirements)
                    {
                        continue;
                    }
                }

                // Step C: Registry lookup
                var candidatePairs = this.registry.GetCandidates(canonicalId.modelPath).ToList();
                Console.WriteLine($"[DEBUG] Registry returned {candidatePairs.Count} candidates for '{canonicalId.modelPath}'");

                // Fallback: sometimes Parse() will split provider/model incorrectly (e.g. when model id contains '/').
                // If nothing was found and we didn't have an explicit canonical config, try the raw candidate string.
                if (candidatePairs.Count == 0 && modelConfig == null)
                {
                    var fallbackMatches = this.registry.GetCandidates(candidateId).ToList();
                    Console.WriteLine($"[DEBUG] Fallback: Registry lookup with '{candidateId}' returned {fallbackMatches.Count} candidates");
                    if (fallbackMatches.Count > 0)
                    {
                        candidatePairs = fallbackMatches;

                        // Reset provider portion since the original parse was likely wrong
                        canonicalId = new CanonicalModelId("unknown", candidateId);
                    }
                }

                // Step D: Provider resolution
                var providers = candidatePairs
                    .Select(p =>
                    {
                        if (this.config.Providers.TryGetValue(p.ServiceKey, out var prov))
                        {
                            // ensure the ProviderConfig knows its key
                            prov.Key = p.ServiceKey;
                            return prov;
                        }

                        return null;
                    })
                    .Where(p => p != null)
                    .Cast<ProviderConfig>()
                    .ToList();

                // Step E: Provider filtering by canonical provider if present
                if (!string.Equals(canonicalId.provider, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    providers = providers
                        .Where(c => string.Equals(c.Key, canonicalId.provider, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Step F: Success check
                if (providers.Count > 0)
                {
                    if (string.Equals(canonicalId.provider, "unknown", StringComparison.OrdinalIgnoreCase))
                    {
                        // Use the first provider as the canonical provider if unknown
                        canonicalId = new CanonicalModelId(providers[0].Key!, canonicalId.modelPath);
                    }

                    return new ResolutionResult(originalModelId, canonicalId, providers);
                }
            }

            // No match found
            var finalCanonicalId = firstCanonicalId ?? CanonicalModelId.Parse(originalModelId);
            return new ResolutionResult(originalModelId, finalCanonicalId, new List<ProviderConfig>());
        }
    }
}

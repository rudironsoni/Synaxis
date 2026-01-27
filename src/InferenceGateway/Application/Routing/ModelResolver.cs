using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;
using System.Collections.Generic;
using System.Linq;
using Synaxis.InferenceGateway.Application.ControlPlane;
using System.Text.Json;

namespace Synaxis.InferenceGateway.Application.Routing;

public class ModelResolver : IModelResolver
{
    private readonly SynaxisConfiguration _config;
    private readonly IProviderRegistry _registry;
    private readonly IControlPlaneStore _store;

    public ModelResolver(IOptions<SynaxisConfiguration> config, IProviderRegistry registry, IControlPlaneStore store)
    {
        _config = config.Value;
        _registry = registry;
        _store = store;
    }

    public ResolutionResult Resolve(string modelId, RequiredCapabilities? required = null)
    {
        // Fallback to config-based for sync callers
        return ResolveConfigOnly(modelId, required);
    }

    public async Task<ResolutionResult> ResolveAsync(string modelId, EndpointKind kind, RequiredCapabilities? required = null, Guid? tenantId = null)
    {
        var candidatesToTry = new List<string>();
        bool foundInDb = false;

        if (tenantId.HasValue)
        {
            // 1. Check Model Aliases
            var alias = await _store.GetAliasAsync(tenantId.Value, modelId);
            string targetModel = alias?.TargetModel ?? modelId;

            // 2. Check Model Combos
            var combo = await _store.GetComboAsync(tenantId.Value, targetModel);

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
                catch { /* Ignore invalid JSON */ }
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
            if (_config.Aliases.TryGetValue(modelId, out var configAlias))
            {
                candidatesToTry.AddRange(configAlias.Candidates);
            }
            else
            {
                candidatesToTry.Add(modelId);
            }
        }

        return ResolveCandidates(modelId, candidatesToTry, required);
    }

    private ResolutionResult ResolveConfigOnly(string modelId, RequiredCapabilities? required)
    {
        var candidatesToTry = new List<string>();
        if (_config.Aliases.TryGetValue(modelId, out var alias))
        {
            candidatesToTry.AddRange(alias.Candidates);
        }
        else
        {
            candidatesToTry.Add(modelId);
        }
        return ResolveCandidates(modelId, candidatesToTry, required);
    }

    private ResolutionResult ResolveCandidates(string originalModelId, List<string> candidates, RequiredCapabilities? required)
    {
        CanonicalModelId? firstCanonicalId = null;

        foreach (var candidateId in candidates)
        {
            // Step A: Canonical Lookup
            var modelConfig = _config.CanonicalModels
                .FirstOrDefault(m => string.Equals(m.Id, candidateId, StringComparison.OrdinalIgnoreCase));

            var canonicalId = modelConfig != null
                ? new CanonicalModelId(modelConfig.Provider, modelConfig.ModelPath)
                : CanonicalModelId.Parse(candidateId);

            firstCanonicalId ??= canonicalId;

            // Step B: Capability check
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
            var candidatePairs = _registry.GetCandidates(canonicalId.ModelPath).ToList();
            
            var providers = candidatePairs
                .Select(p => {
                    if (_config.Providers.TryGetValue(p.ServiceKey, out var prov))
                    {
                        prov.Key = p.ServiceKey;
                        return prov;
                    }
                    return null;
                })
                .Where(p => p != null)
                .Cast<ProviderConfig>()
                .ToList();

            // Step D: Provider filtering
            if (canonicalId.Provider != "unknown")
            {
                providers = providers
                    .Where(c => string.Equals(c.Key, canonicalId.Provider, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Step E: Success check
            if (providers.Count > 0)
            {
                if (canonicalId.Provider == "unknown")
                {
                    canonicalId = new CanonicalModelId(providers[0].Key!, canonicalId.ModelPath);
                }
                return new ResolutionResult(originalModelId, canonicalId, providers);
            }
        }

        // No match found
        var finalCanonicalId = firstCanonicalId ?? CanonicalModelId.Parse(originalModelId);
        return new ResolutionResult(originalModelId, finalCanonicalId, new List<ProviderConfig>());
    }
}
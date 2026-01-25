using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Synaxis.InferenceGateway.Application.Routing;

public class ModelResolver : IModelResolver
{
    private readonly SynaxisConfiguration _config;
    private readonly IProviderRegistry _registry;

    public ModelResolver(IOptions<SynaxisConfiguration> config, IProviderRegistry registry)
    {
        _config = config.Value;
        _registry = registry;
    }

    public ResolutionResult Resolve(string modelId, RequiredCapabilities? required = null)
    {
        // 1. Get candidates to try
        var candidatesToTry = new List<string>();
        if (_config.Aliases.TryGetValue(modelId, out var alias))
        {
            candidatesToTry.AddRange(alias.Candidates);
        }
        else
        {
            candidatesToTry.Add(modelId);
        }

        CanonicalModelId? firstCanonicalId = null;

        foreach (var candidateId in candidatesToTry)
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
                return new ResolutionResult(modelId, canonicalId, providers);
            }
        }

        // No match found
        var finalCanonicalId = firstCanonicalId ?? CanonicalModelId.Parse(modelId);
        return new ResolutionResult(modelId, finalCanonicalId, new List<ProviderConfig>());
    }

    public Task<ResolutionResult> ResolveAsync(string modelId, EndpointKind kind, RequiredCapabilities? required = null)
    {
        // For now, synchronous resolution wrapped in Task
        return Task.FromResult(Resolve(modelId, required));
    }
}

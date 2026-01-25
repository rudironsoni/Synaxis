using Microsoft.Extensions.Options;
using Synaxis.Application.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Synaxis.Application.Routing;

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
        string targetModelId = modelId;
        
        // 1. Alias check
        if (_config.Aliases.TryGetValue(modelId, out var alias))
        {
            targetModelId = alias.Target;
        }

        var canonicalId = CanonicalModelId.Parse(targetModelId);
        
        // 2. Capability check
        if (required != null)
        {
            var modelConfig = _config.CanonicalModels.FirstOrDefault(m => m.Id == targetModelId);
            if (modelConfig != null)
            {
                bool meetsRequirements = 
                    (!required.Streaming || modelConfig.Streaming) &&
                    (!required.Tools || modelConfig.Tools) &&
                    (!required.Vision || modelConfig.Vision) &&
                    (!required.StructuredOutput || modelConfig.StructuredOutput) &&
                    (!required.LogProbs || modelConfig.LogProbs);
                    
                if (!meetsRequirements)
                {
                    return new ResolutionResult(modelId, canonicalId, new List<ProviderConfig>());
                }
            }
        }

        // 3. Get candidates from registry
        var candidatePairs = _registry.GetCandidates(targetModelId).ToList();
        
        var candidates = candidatePairs
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

        return new ResolutionResult(modelId, canonicalId, candidates);
    }

    public Task<ResolutionResult> ResolveAsync(string modelId, EndpointKind kind, RequiredCapabilities? required = null)
    {
        // For now, synchronous resolution wrapped in Task
        return Task.FromResult(Resolve(modelId, required));
    }
}

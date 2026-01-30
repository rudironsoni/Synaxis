using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Synaxis.InferenceGateway.Application.Routing;

public class SmartRouter : ISmartRouter
{
    private readonly IModelResolver _modelResolver;
    private readonly ICostService _costService;
    private readonly IHealthStore _healthStore;
    private readonly IQuotaTracker _quotaTracker;
    private readonly ILogger<SmartRouter> _logger;

    public SmartRouter(
        IModelResolver modelResolver,
        ICostService costService,
        IHealthStore healthStore,
        IQuotaTracker quotaTracker,
        ILogger<SmartRouter> logger)
    {
        _modelResolver = modelResolver;
        _costService = costService;
        _healthStore = healthStore;
        _quotaTracker = quotaTracker;
        _logger = logger;
    }

    public async Task<List<EnrichedCandidate>> GetCandidatesAsync(string modelId, bool streaming, CancellationToken cancellationToken = default)
    {
        var caps = new RequiredCapabilities { Streaming = streaming };
        var resolution = await _modelResolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, caps);

        if (resolution.Candidates.Count == 0)
        {
             _logger.LogWarning("No providers found for model '{ModelId}' with required capabilities.", modelId);
             throw new ArgumentException($"No providers available for model '{modelId}' with the requested capabilities.");
        }

        var enriched = new List<EnrichedCandidate>();
        foreach (var candidate in resolution.Candidates)
        {
             if (!await _healthStore.IsHealthyAsync(candidate.Key!, cancellationToken)) 
             {
                 _logger.LogDebug("Skipping unhealthy provider '{ProviderKey}'", candidate.Key);
                 continue;
             }
             
             if (!await _quotaTracker.CheckQuotaAsync(candidate.Key!, cancellationToken)) 
             {
                 _logger.LogDebug("Skipping quota-exceeded provider '{ProviderKey}'", candidate.Key);
                 continue;
             }

              var cost = await _costService.GetCostAsync(candidate.Key!, resolution.CanonicalId.ModelPath, cancellationToken);
              enriched.Add(new EnrichedCandidate(candidate, cost, resolution.CanonicalId.ModelPath));
        }

        return enriched
            .OrderByDescending(c => c.IsFree)
            .ThenBy(c => c.CostPerToken)
            .ThenBy(c => c.Config.Tier)
            .ToList();
    }
}

using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Application.Routing;

/// <summary>
/// Implements multi-tier fallback orchestration for provider selection.
/// </summary>
public class FallbackOrchestrator : IFallbackOrchestrator
{
    private readonly ISmartRouter _smartRouter;
    private readonly IQuotaTracker _quotaTracker;
    private readonly IHealthStore _healthStore;
    private readonly ILogger<FallbackOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the FallbackOrchestrator.
    /// </summary>
    /// <param name="smartRouter">The smart router for candidate selection</param>
    /// <param name="quotaTracker">The quota tracker for checking provider quotas</param>
    /// <param name="healthStore">The health store for checking provider health</param>
    /// <param name="logger">The logger for diagnostic information</param>
    public FallbackOrchestrator(
        ISmartRouter smartRouter,
        IQuotaTracker quotaTracker,
        IHealthStore healthStore,
        ILogger<FallbackOrchestrator> logger)
    {
        _smartRouter = smartRouter;
        _quotaTracker = quotaTracker;
        _healthStore = healthStore;
        _logger = logger;
    }

    /// <summary>
    /// Executes a request with intelligent multi-tier fallback.
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="modelId">The model identifier</param>
    /// <param name="streaming">Whether streaming is required</param>
    /// <param name="preferredProviderKey">Optional user-preferred provider key</param>
    /// <param name="operation">The operation to execute with the selected provider</param>
    /// <param name="tenantId">Optional tenant ID for routing configuration</param>
    /// <param name="userId">Optional user ID for routing configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<T> ExecuteWithFallbackAsync<T>(
        string modelId,
        bool streaming,
        string? preferredProviderKey,
        Func<EnrichedCandidate, Task<T>> operation,
        string? tenantId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Tier 1: User preferred provider
        if (!string.IsNullOrEmpty(preferredProviderKey))
        {
            _logger.LogInformation("Attempting Tier 1 fallback: User preferred provider '{ProviderKey}'", preferredProviderKey);
            try
            {
                var candidates = await _smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken);
                var preferred = candidates.FirstOrDefault(c => c.Key == preferredProviderKey);

                if (preferred != null &&
                    await _healthStore.IsHealthyAsync(preferred.Key!, cancellationToken) &&
                    await _quotaTracker.CheckQuotaAsync(preferred.Key!, cancellationToken))
                {
                    _logger.LogInformation("Using user preferred provider '{ProviderKey}'", preferred.Key);
                    return await operation(preferred);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "User preferred provider '{ProviderKey}' failed", preferredProviderKey);
            }
        }

        // Tier 2: Free tier providers
        _logger.LogInformation("Attempting Tier 2 fallback: Free tier providers");
        try
        {
            var candidates = await _smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken);
            var freeProviders = candidates.Where(c => c.IsFree).ToList();

            foreach (var provider in freeProviders)
            {
                try
                {
                    if (await _healthStore.IsHealthyAsync(provider.Key!, cancellationToken) &&
                        await _quotaTracker.CheckQuotaAsync(provider.Key!, cancellationToken))
                    {
                        _logger.LogInformation("Using free tier provider '{ProviderKey}'", provider.Key);
                        return await operation(provider);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Free tier provider '{ProviderKey}' failed", provider.Key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Free tier fallback failed");
        }

        // Tier 3: Paid providers
        _logger.LogInformation("Attempting Tier 3 fallback: Paid providers");
        try
        {
            var candidates = await _smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken);
            var paidProviders = candidates.Where(c => !c.IsFree).ToList();

            foreach (var provider in paidProviders)
            {
                try
                {
                    if (await _healthStore.IsHealthyAsync(provider.Key!, cancellationToken) &&
                        await _quotaTracker.CheckQuotaAsync(provider.Key!, cancellationToken))
                    {
                        _logger.LogInformation("Using paid provider '{ProviderKey}'", provider.Key);
                        return await operation(provider);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Paid provider '{ProviderKey}' failed", provider.Key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Paid provider fallback failed");
        }

        // Tier 4: Emergency fallback - any healthy provider
        _logger.LogInformation("Attempting Tier 4 fallback: Emergency fallback");
        var allCandidates = await _smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken);
        foreach (var provider in allCandidates)
        {
            try
            {
                if (await _healthStore.IsHealthyAsync(provider.Key!, cancellationToken))
                {
                    _logger.LogWarning("Emergency fallback using provider '{ProviderKey}'", provider.Key);
                    return await operation(provider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Emergency fallback provider '{ProviderKey}' failed", provider.Key);
            }
        }

        throw new InvalidOperationException($"All fallback tiers failed for model '{modelId}'. No healthy providers available.");
    }
}

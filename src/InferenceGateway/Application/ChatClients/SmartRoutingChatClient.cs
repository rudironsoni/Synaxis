using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.Routing;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Polly;
using Polly.Registry;

namespace Synaxis.InferenceGateway.Application.ChatClients;

public class SmartRoutingChatClient : IChatClient
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IModelResolver _modelResolver;
    private readonly ICostService _costService;
    private readonly IHealthStore _healthStore;
    private readonly IQuotaTracker _quotaTracker;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<SmartRoutingChatClient> _logger;

    public ChatClientMetadata Metadata { get; } = new("SmartRoutingChatClient");

    public SmartRoutingChatClient(
        IServiceProvider serviceProvider,
        IModelResolver modelResolver,
        ICostService costService,
        IHealthStore healthStore,
        IQuotaTracker quotaTracker,
        ResiliencePipelineProvider<string> pipelineProvider,
        ActivitySource activitySource,
        ILogger<SmartRoutingChatClient> logger)
    {
        _serviceProvider = serviceProvider;
        _modelResolver = modelResolver;
        _costService = costService;
        _healthStore = healthStore;
        _quotaTracker = quotaTracker;
        _pipelineProvider = pipelineProvider;
        _activitySource = activitySource;
        _logger = logger;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ChatRequest");
        var modelId = options?.ModelId ?? "default";
        activity?.SetTag("model.id", modelId);

        var resolution = await ResolveModelAsync(modelId, false, cancellationToken);

        if (resolution.Candidates.Count == 0)
        {
            throw new InvalidOperationException($"No providers found for model '{modelId}'.");
        }

        var sortedCandidates = await SortCandidatesAsync(resolution.Candidates, cancellationToken);

        var exceptions = new List<Exception>();

        foreach (var candidate in sortedCandidates)
        {
            try
            {
                activity?.SetTag("provider.selected", candidate.Key);
                var client = _serviceProvider.GetKeyedService<IChatClient>(candidate.Key);
                if (client == null)
                {
                    _logger.LogWarning("Provider '{ProviderKey}' resolved but not registered.", candidate.Key);
                    continue;
                }

                _logger.LogInformation("Routing request to provider '{ProviderKey}' (Cost: {Cost}, Free: {IsFree})",
                    candidate.Key, candidate.CostPerToken, candidate.IsFree);

                var pipeline = _pipelineProvider.GetPipeline("provider-retry");
                var response = await pipeline.ExecuteAsync(async ct =>
                    await client.GetResponseAsync(chatMessages.ToList(), options, ct), cancellationToken);

                try
                {
                    await _healthStore.MarkSuccessAsync(candidate.Key, cancellationToken);
                    await _quotaTracker.RecordUsageAsync(candidate.Key, response.Usage?.InputTokenCount ?? 0, response.Usage?.OutputTokenCount ?? 0, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record success/usage metrics for provider '{ProviderKey}'. Ignoring.", candidate.Key);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider '{ProviderKey}' failed.", candidate.Key);
                // Try to mark failure, but don't crash if health store fails
                try { await _healthStore.MarkFailureAsync(candidate.Key, TimeSpan.FromSeconds(30), cancellationToken); } catch { }
                exceptions.Add(ex);
            }
        }

        var errorMsg = string.Join("; ", exceptions.Select(e => $"{e.GetType().Name}: {e.Message}"));
        throw new AggregateException($"All providers failed for model '{modelId}'. Errors: {errorMsg}", exceptions);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ChatRequest.Streaming");
        var modelId = options?.ModelId ?? "default";
        activity?.SetTag("model.id", modelId);

        var resolution = await ResolveModelAsync(modelId, true, cancellationToken);

        if (resolution.Candidates.Count == 0)
        {
            throw new InvalidOperationException($"No providers found for model '{modelId}'.");
        }

        var sortedCandidates = await SortCandidatesAsync(resolution.Candidates, cancellationToken);
        var exceptions = new List<Exception>();
        bool successfulStream = false;

        foreach (var candidate in sortedCandidates)
        {
            activity?.SetTag("provider.selected", candidate.Key);
            IAsyncEnumerable<ChatResponseUpdate>? stream = null;
            try
            {
                var client = _serviceProvider.GetKeyedService<IChatClient>(candidate.Key);
                if (client == null) continue;

                _logger.LogInformation("Routing streaming request to provider '{ProviderKey}'", candidate.Key);

                var pipeline = _pipelineProvider.GetPipeline("provider-retry");
                stream = await pipeline.ExecuteAsync(async ct =>
                    client.GetStreamingResponseAsync(chatMessages.ToList(), options, ct), cancellationToken);

                try
                {
                    await _healthStore.MarkSuccessAsync(candidate.Key, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record stream success for provider '{ProviderKey}'. Ignoring.", candidate.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate stream for provider '{ProviderKey}'", candidate.Key);
                await _healthStore.MarkFailureAsync(candidate.Key, TimeSpan.FromSeconds(30), cancellationToken);
                exceptions.Add(ex);
                continue;
            }

            if (stream != null)
            {
                successfulStream = true;
                // We must await inside the loop to ensure the stream is processed.
                // If an exception occurs DURING iteration, we throw and abort (fail-fast once committed).
                await foreach (var update in stream.WithCancellation(cancellationToken))
                {
                    yield return update;
                }
                yield break; // Success
            }
        }

        if (!successfulStream)
        {
            var errorMsg = string.Join("; ", exceptions.Select(e => $"{e.GetType().Name}: {e.Message}"));
            throw new AggregateException($"All providers failed to initiate stream for model '{modelId}'. Errors: {errorMsg}", exceptions);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return _serviceProvider.GetKeyedService(serviceType, serviceKey);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    private async Task<ResolutionResult> ResolveModelAsync(string modelId, bool streaming, CancellationToken cancellationToken)
    {
        var caps = new RequiredCapabilities { Streaming = streaming };
        return await _modelResolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, caps);
    }

    private async Task<List<EnrichedCandidate>> SortCandidatesAsync(IEnumerable<ProviderConfig> candidates, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("SortCandidates");
        var enriched = new List<EnrichedCandidate>();

        try
        {
            foreach (var candidate in candidates)
            {
                // Pre-routing checks: Health and Quota
                try
                {
                    if (!await _healthStore.IsHealthyAsync(candidate.Key!, cancellationToken))
                    {
                        _logger.LogDebug("Skipping unhealthy provider '{ProviderKey}'", candidate.Key);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking health for provider '{ProviderKey}'. Treating as HEALTHY (Fail Open).", candidate.Key);
                    // Fail Open: Proceed despite error
                }

                try
                {
                    if (!await _quotaTracker.CheckQuotaAsync(candidate.Key!, cancellationToken))
                    {
                        _logger.LogDebug("Skipping provider '{ProviderKey}' due to quota limits", candidate.Key);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking quota for provider '{ProviderKey}'. Treating as ALLOWED (Fail Open).", candidate.Key);
                    // Fail Open: Proceed despite error
                }

                Application.ControlPlane.Entities.ModelCost? cost = null;
                try
                {
                    cost = await _costService.GetCostAsync(candidate.Key!, "default", cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting cost for provider '{ProviderKey}'. Treating as unknown cost.", candidate.Key);
                    // Fail Open: Proceed with null cost
                }

                enriched.Add(new EnrichedCandidate(candidate, cost));
            }

            return enriched
                .OrderByDescending(c => c.IsFree)
                .ThenBy(c => c.CostPerToken)
                .ThenBy(c => c.Config.Tier)
                .ThenBy(_ => Guid.NewGuid())
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during candidate sorting.");
            throw;
        }
    }

    private record EnrichedCandidate(ProviderConfig Config, Application.ControlPlane.Entities.ModelCost? Cost)
    {
        public string Key => Config.Key!;
        public bool IsFree => Cost?.FreeTier ?? false;
        public decimal CostPerToken => Cost?.CostPerToken ?? decimal.MaxValue;

        // Suppress unused parameter warning for Config if it occurs in some compiler versions
        // though it is used in the Key property above.
    }
}

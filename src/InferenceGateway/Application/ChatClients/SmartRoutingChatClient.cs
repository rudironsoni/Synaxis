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

        var candidates = await GetCandidatesAsync(modelId, false, cancellationToken);
        var exceptions = new List<Exception>();

        foreach (var candidate in candidates)
        {
            try
            {
                var response = await ExecuteCandidateAsync(candidate, chatMessages, options, cancellationToken);

                // Add Metadata
                if (response.AdditionalProperties == null) response.AdditionalProperties = new AdditionalPropertiesDictionary();
                response.AdditionalProperties["provider_name"] = candidate.Key;
                response.AdditionalProperties["model_id"] = candidate.CanonicalModelPath;

                return response;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException($"All providers failed for model '{modelId}'.", exceptions);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ChatRequest.Streaming");
        var modelId = options?.ModelId ?? "default";
        activity?.SetTag("model.id", modelId);

        var candidates = await GetCandidatesAsync(modelId, true, cancellationToken);
        var exceptions = new List<Exception>();

        IAsyncEnumerable<ChatResponseUpdate>? successfulStream = null;
        EnrichedCandidate? successfulCandidate = null;

        foreach (var candidate in candidates)
        {
            try
            {
                successfulStream = await ExecuteCandidateStreamingAsync(candidate, chatMessages, options, cancellationToken);
                successfulCandidate = candidate;
                break;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (successfulStream == null)
        {
            throw new AggregateException($"All providers failed to initiate stream for model '{modelId}'.", exceptions);
        }

        await foreach (var update in successfulStream.WithCancellation(cancellationToken))
        {
             if (successfulCandidate != null)
             {
                 if (update.AdditionalProperties == null) update.AdditionalProperties = new AdditionalPropertiesDictionary();
                 update.AdditionalProperties["provider_name"] = successfulCandidate.Key;
                 update.AdditionalProperties["model_id"] = successfulCandidate.CanonicalModelPath;
             }
             yield return update;
        }
    }

    private async Task<ChatResponse> ExecuteCandidateAsync(
        EnrichedCandidate candidate,
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? originalOptions,
        CancellationToken cancellationToken)
    {
        var client = _serviceProvider.GetKeyedService<IChatClient>(candidate.Key)
                     ?? throw new InvalidOperationException($"Provider '{candidate.Key}' not registered.");

        _logger.LogInformation("Routing request to provider '{ProviderKey}'", candidate.Key);

        var routedOptions = originalOptions?.Clone() ?? new ChatOptions();
        routedOptions.ModelId = candidate.CanonicalModelPath;

        var pipeline = _pipelineProvider.GetPipeline("provider-retry");

        try
        {
            var response = await pipeline.ExecuteAsync(async ct =>
                await client.GetResponseAsync(chatMessages.ToList(), routedOptions, ct), cancellationToken);

            await RecordMetricsAsync(candidate.Key, response.Usage?.InputTokenCount ?? 0, response.Usage?.OutputTokenCount ?? 0, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(candidate.Key, ex, cancellationToken);
            throw;
        }
    }

    private async Task<IAsyncEnumerable<ChatResponseUpdate>> ExecuteCandidateStreamingAsync(
        EnrichedCandidate candidate,
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? originalOptions,
        CancellationToken cancellationToken)
    {
         var client = _serviceProvider.GetKeyedService<IChatClient>(candidate.Key)
                     ?? throw new InvalidOperationException($"Provider '{candidate.Key}' not registered.");

        _logger.LogInformation("Routing streaming request to provider '{ProviderKey}'", candidate.Key);

        var routedOptions = originalOptions?.Clone() ?? new ChatOptions();
        routedOptions.ModelId = candidate.CanonicalModelPath;

        var pipeline = _pipelineProvider.GetPipeline("provider-retry");

        try
        {
            // Use synchronous delegate for returning the stream, wrapped in ValueTask
            return await pipeline.ExecuteAsync(ct =>
            {
                var stream = client.GetStreamingResponseAsync(chatMessages.ToList(), routedOptions, ct);
                return new ValueTask<IAsyncEnumerable<ChatResponseUpdate>>(stream);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(candidate.Key, ex, cancellationToken);
            throw;
        }
    }

    private async Task<List<EnrichedCandidate>> GetCandidatesAsync(string modelId, bool streaming, CancellationToken cancellationToken)
    {
        var caps = new RequiredCapabilities { Streaming = streaming };
        var resolution = await _modelResolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, caps);

        if (resolution.Candidates.Count == 0)
        {
             throw new InvalidOperationException($"No providers found for model '{modelId}'.");
        }

        var enriched = new List<EnrichedCandidate>();
        foreach (var candidate in resolution.Candidates)
        {
             if (!await _healthStore.IsHealthyAsync(candidate.Key!, cancellationToken)) continue;
             if (!await _quotaTracker.CheckQuotaAsync(candidate.Key!, cancellationToken)) continue;

             var cost = await _costService.GetCostAsync(candidate.Key!, "default", cancellationToken);
             enriched.Add(new EnrichedCandidate(candidate, cost, resolution.CanonicalId.ModelPath));
        }

        return enriched
            .OrderByDescending(c => c.IsFree)
            .ThenBy(c => c.CostPerToken)
            .ThenBy(c => c.Config.Tier)
            .ToList();
    }

    private async Task RecordMetricsAsync(string providerKey, long inputTokens, long outputTokens, CancellationToken cancellationToken)
    {
        try
        {
            await _healthStore.MarkSuccessAsync(providerKey, cancellationToken);
            if (inputTokens > 0 || outputTokens > 0)
            {
                await _quotaTracker.RecordUsageAsync(providerKey, inputTokens, outputTokens, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record metrics for provider '{ProviderKey}'.", providerKey);
        }
    }

    private async Task RecordFailureAsync(string providerKey, Exception ex, CancellationToken cancellationToken)
    {
        _logger.LogError(ex, "Provider '{ProviderKey}' failed.", providerKey);
        try
        {
            await _healthStore.MarkFailureAsync(providerKey, TimeSpan.FromSeconds(30), cancellationToken);
        }
        catch { }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => _serviceProvider.GetKeyedService(serviceType, serviceKey);

    public void Dispose() { }

    private record EnrichedCandidate(ProviderConfig Config, Application.ControlPlane.Entities.ModelCost? Cost, string CanonicalModelPath)
    {
        public string Key => Config.Key!;
        public bool IsFree => Cost?.FreeTier ?? false;
        public decimal CostPerToken => Cost?.CostPerToken ?? decimal.MaxValue;
    }
}

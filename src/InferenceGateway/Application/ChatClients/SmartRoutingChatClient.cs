using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.Routing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;

namespace Synaxis.InferenceGateway.Application.ChatClients;

public class SmartRoutingChatClient : IChatClient
{
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ISmartRouter _smartRouter;
    private readonly IHealthStore _healthStore;
    private readonly IQuotaTracker _quotaTracker;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<SmartRoutingChatClient> _logger;

    public ChatClientMetadata Metadata { get; } = new("SmartRoutingChatClient");

    public SmartRoutingChatClient(
        IChatClientFactory chatClientFactory,
        ISmartRouter smartRouter,
        IHealthStore healthStore,
        IQuotaTracker quotaTracker,
        ResiliencePipelineProvider<string> pipelineProvider,
        ActivitySource activitySource,
        ILogger<SmartRoutingChatClient> logger)
    {
        _chatClientFactory = chatClientFactory;
        _smartRouter = smartRouter;
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

        // 1. Get Candidates from Router (Policy Layer)
        var candidates = await _smartRouter.GetCandidatesAsync(modelId, false, cancellationToken);
        var exceptions = new List<Exception>();

        // 2. Rotation Loop (Resilience Layer)
        foreach (var candidate in candidates)
        {
            // Fast check: Is this provider explicitly blocked or quota-out?
            if (!await _quotaTracker.IsHealthyAsync(candidate.Key, cancellationToken)) 
            {
                _logger.LogDebug("Skipping provider '{ProviderKey}' due to quota/health check.", candidate.Key);
                continue;
            }

            try
            {
                return await ExecuteCandidateAsync(candidate, chatMessages, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider '{ProviderKey}' failed. Rotating to next candidate.", candidate.Key);
                exceptions.Add(ex);
                // Continue loop to next candidate
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

        var candidates = await _smartRouter.GetCandidatesAsync(modelId, true, cancellationToken);
        var exceptions = new List<Exception>();

        IAsyncEnumerable<ChatResponseUpdate>? successfulStream = null;
        EnrichedCandidate? successfulCandidate = null;

        // Rotation Loop for Streaming
        foreach (var candidate in candidates)
        {
            if (!await _quotaTracker.IsHealthyAsync(candidate.Key, cancellationToken)) continue;

            try
            {
                successfulStream = await ExecuteCandidateStreamingAsync(candidate, chatMessages, options, cancellationToken);
                successfulCandidate = candidate;
                break; // Stream initiated successfully
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider '{ProviderKey}' failed to initiate stream. Rotating...", candidate.Key);
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
        var client = _chatClientFactory.GetClient(candidate.Key)
                     ?? throw new InvalidOperationException($"Provider '{candidate.Key}' not registered.");

        _logger.LogInformation("Routing request to provider '{ProviderKey}'", candidate.Key);

        var routedOptions = originalOptions?.Clone() ?? new ChatOptions();
        routedOptions.ModelId = candidate.CanonicalModelPath;

        var pipeline = _pipelineProvider.GetPipeline("provider-retry");

        try
        {
            var response = await pipeline.ExecuteAsync(async ct =>
                await client.GetResponseAsync(chatMessages.ToList(), routedOptions, ct), cancellationToken);

            // Add Routing Metadata
            if (response.AdditionalProperties == null) response.AdditionalProperties = new AdditionalPropertiesDictionary();
            response.AdditionalProperties["provider_name"] = candidate.Key;
            response.AdditionalProperties["model_id"] = candidate.CanonicalModelPath;

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
         var client = _chatClientFactory.GetClient(candidate.Key)
                     ?? throw new InvalidOperationException($"Provider '{candidate.Key}' not registered.");

        _logger.LogInformation("Routing streaming request to provider '{ProviderKey}'", candidate.Key);

        var routedOptions = originalOptions?.Clone() ?? new ChatOptions();
        routedOptions.ModelId = candidate.CanonicalModelPath;

        var pipeline = _pipelineProvider.GetPipeline("provider-retry");

        try
        {
            // We await the *creation* of the stream inside the resilience pipeline
            // If connection fails, pipeline retries. If stream starts, we return it.
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
        try
        {
            await _healthStore.MarkFailureAsync(providerKey, TimeSpan.FromSeconds(30), cancellationToken);
        }
        catch { }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => _chatClientFactory.GetService(serviceType, serviceKey);

    public void Dispose() { }
}

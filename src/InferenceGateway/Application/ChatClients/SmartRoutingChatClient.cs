using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.ChatClients.Strategies;
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
    private readonly IEnumerable<IChatClientStrategy> _strategies;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<SmartRoutingChatClient> _logger;

    public ChatClientMetadata Metadata { get; } = new("SmartRoutingChatClient");

    public SmartRoutingChatClient(
        IChatClientFactory chatClientFactory,
        ISmartRouter smartRouter,
        IHealthStore healthStore,
        IQuotaTracker quotaTracker,
        ResiliencePipelineProvider<string> pipelineProvider,
        IEnumerable<IChatClientStrategy> strategies,
        ActivitySource activitySource,
        ILogger<SmartRoutingChatClient> logger)
    {
        _chatClientFactory = chatClientFactory;
        _smartRouter = smartRouter;
        _healthStore = healthStore;
        _quotaTracker = quotaTracker;
        _pipelineProvider = pipelineProvider;
        _strategies = strategies;
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
                try { await RecordFailureAsync(candidate.Key, modelId, ex, cancellationToken); } catch { }
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
                try { await RecordFailureAsync(candidate.Key, modelId, ex, cancellationToken); } catch { }
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
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(candidate.Config.Type)) 
                       ?? _strategies.First();

        try
        {
            var response = await pipeline.ExecuteAsync(async ct =>
                await strategy.ExecuteAsync(client, chatMessages.ToList(), routedOptions, ct), cancellationToken);

            // Add Routing Metadata
            if (response.AdditionalProperties == null) response.AdditionalProperties = new AdditionalPropertiesDictionary();
            response.AdditionalProperties["provider_name"] = candidate.Key;
            response.AdditionalProperties["model_id"] = candidate.CanonicalModelPath;

            await RecordMetricsAsync(candidate.Key, response.Usage?.InputTokenCount ?? 0, response.Usage?.OutputTokenCount ?? 0, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(candidate.Key, candidate.CanonicalModelPath, ex, cancellationToken);
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
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(candidate.Config.Type)) 
                       ?? _strategies.First();

        try
        {
            // We await the *creation* of the stream inside the resilience pipeline
            // If connection fails, pipeline retries. If stream starts, we return it.
            return await pipeline.ExecuteAsync(ct =>
            {
                var stream = strategy.ExecuteStreamingAsync(client, chatMessages.ToList(), routedOptions, ct);
                return new ValueTask<IAsyncEnumerable<ChatResponseUpdate>>(stream);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(candidate.Key, candidate.CanonicalModelPath, ex, cancellationToken);
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

        private async Task RecordFailureAsync(string providerKey, string? modelId, Exception ex, CancellationToken cancellationToken)
        {
            try
            {
                // Inspect exception to determine appropriate circuit-break cooldown and logging severity
                int? statusCode = null;

                // Try well-known exception types first
                try
                {
                    // Some AI exceptions (e.g. Microsoft.Extensions.AI.AIException) may contain a StatusCode or Status property
                    // Use reflection to avoid compile-time dependency on that type.
                    var exType = ex.GetType();
                    if (exType.Name == "AIException" || exType.FullName == "Microsoft.Extensions.AI.AIException")
                    {
                        // Use robust reflection to avoid AmbiguousMatchException; prefer known numeric/http status types
                        var statusProp = exType.GetProperties()
                            .FirstOrDefault(p => (string.Equals(p.Name, "StatusCode", StringComparison.OrdinalIgnoreCase)
                                                  || string.Equals(p.Name, "Status", StringComparison.OrdinalIgnoreCase))
                                                 && (p.PropertyType == typeof(int)
                                                     || p.PropertyType == typeof(System.Net.HttpStatusCode)
                                                     || p.PropertyType == typeof(System.Net.HttpStatusCode)));
                        if (statusProp != null)
                        {
                            var val = statusProp.GetValue(ex);
                            if (val is System.Net.HttpStatusCode sc) statusCode = (int)sc;
                            else if (val is int i) statusCode = i;
                        }
                    }

                    // HttpRequestException (newer runtimes) may expose a StatusCode property
                    if (statusCode == null && ex is System.Net.Http.HttpRequestException httpEx)
                    {
                        var t = httpEx.GetType();
                        var prop = t.GetProperty("StatusCode");
                        if (t.GetProperties().Length > 0)
                        {
                            var p = t.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "StatusCode", StringComparison.OrdinalIgnoreCase)
                                                                          && (p.PropertyType == typeof(System.Net.HttpStatusCode) || p.PropertyType == typeof(int)));
                            if (p != null)
                            {
                                var val = p.GetValue(httpEx);
                                if (val is System.Net.HttpStatusCode sc) statusCode = (int)sc;
                                else if (val is int i) statusCode = i;
                            }
                        }
                    }

                    // Walk inner exceptions to find a status code if present
                    var inner = ex.InnerException;
                    while (statusCode == null && inner != null)
                    {
                            if (inner is System.Net.Http.HttpRequestException innerHttp)
                            {
                                var t = innerHttp.GetType();
                                var p = t.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "StatusCode", StringComparison.OrdinalIgnoreCase)
                                                                              && (p.PropertyType == typeof(System.Net.HttpStatusCode) || p.PropertyType == typeof(int)));
                                if (p != null)
                                {
                                    var val = p.GetValue(innerHttp);
                                    if (val is System.Net.HttpStatusCode sc) statusCode = (int)sc;
                                    else if (val is int i) statusCode = i;
                                }
                            }
                        else
                        {
                            // Try to extract numeric status code from message (best-effort)
                            if (statusCode == null)
                            {
                                var m = System.Text.RegularExpressions.Regex.Match(inner.Message ?? string.Empty, "(4\\d{2}|5\\d{2}|401|429)");
                                if (m.Success && int.TryParse(m.Value, out var parsed)) statusCode = parsed;
                            }
                        }

                        inner = inner.InnerException;
                    }
                }
                catch { /* best-effort inspection should not throw */ }

                // Default behavior: treat as 5xx provider error
                var cooldown = TimeSpan.FromSeconds(30);

                if (statusCode.HasValue)
                {
                    var code = statusCode.Value;
                    if (code == 429)
                    {
                        cooldown = TimeSpan.FromSeconds(60);
                        _logger.LogWarning(ex, "Provider '{ProviderKey}' returned 429 Too Many Requests for model '{ModelId}'. StatusCode: {StatusCode}", providerKey, modelId ?? "unknown", code);
                    }
                    else if (code == 401)
                    {
                        cooldown = TimeSpan.FromHours(1);
                        _logger.LogCritical(ex, "Provider '{ProviderKey}' returned 401 Unauthorized for model '{ModelId}'. StatusCode: {StatusCode}", providerKey, modelId ?? "unknown", code);
                    }
                    else if (code == 400 || code == 404)
                    {
                        // Do not penalize provider for model/input errors
                        _logger.LogError(ex, "Provider '{ProviderKey}' returned {StatusCode} (model/input error) for model '{ModelId}'. Not marking provider as failed.", providerKey, code, modelId ?? "unknown");
                        return;
                    }
                    else if (code >= 500 && code < 600)
                    {
                        cooldown = TimeSpan.FromSeconds(30);
                        _logger.LogError(ex, "Provider '{ProviderKey}' returned {StatusCode} server error for model '{ModelId}'. Marking provider with cooldown {Cooldown}s.", providerKey, code, modelId ?? "unknown", cooldown.TotalSeconds);
                    }
                    else
                    {
                        // Unknown status code - treat as transient provider error
                        cooldown = TimeSpan.FromSeconds(30);
                        _logger.LogError(ex, "Provider '{ProviderKey}' returned unexpected status code {StatusCode} for model '{ModelId}'. Applying default cooldown {Cooldown}s.", providerKey, statusCode.Value, modelId ?? "unknown", cooldown.TotalSeconds);
                    }
                }
                else
                {
                    // No status code found - fallback
                    cooldown = TimeSpan.FromSeconds(30);
                    _logger.LogError(ex, "Provider '{ProviderKey}' failed for model '{ModelId}' with no HTTP status code. Applying default cooldown {Cooldown}s.", providerKey, modelId ?? "unknown", cooldown.TotalSeconds);
                }

                await _healthStore.MarkFailureAsync(providerKey, cooldown, cancellationToken);
            }
            catch { }
        }

    public object? GetService(Type serviceType, object? serviceKey = null) => _chatClientFactory.GetService(serviceType, serviceKey);

    public void Dispose() { }
}

// <copyright file="SmartRoutingChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ChatClients
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.Logging;
    using Polly;
    using Polly.Registry;
    using Synaxis.InferenceGateway.Application.ChatClients.Strategies;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.Routing;

    /// <summary>
    /// Chat client that implements smart routing with fallback.
    /// </summary>
    public sealed class SmartRoutingChatClient : IChatClient
    {
        private static readonly Regex StatusCodeRegex = new("(4\\d{2}|5\\d{2}|401|429)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));

        private readonly IChatClientFactory chatClientFactory;
        private readonly IHealthStore healthStore;
        private readonly IQuotaTracker quotaTracker;
        private readonly ResiliencePipelineProvider<string> pipelineProvider;
        private readonly IEnumerable<IChatClientStrategy> strategies;
        private readonly ActivitySource activitySource;
        private readonly IFallbackOrchestrator fallbackOrchestrator;
        private readonly ILogger<SmartRoutingChatClient> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartRoutingChatClient"/> class.
        /// </summary>
        /// <param name="chatClientFactory">The chat client factory.</param>
        /// <param name="smartRouter">The smart router.</param>
        /// <param name="healthStore">The health store.</param>
        /// <param name="quotaTracker">The quota tracker.</param>
        /// <param name="pipelineProvider">The resilience pipeline provider.</param>
        /// <param name="strategies">The chat client strategies.</param>
        /// <param name="activitySource">The activity source.</param>
        /// <param name="fallbackOrchestrator">The fallback orchestrator.</param>
        /// <param name="logger">The logger instance.</param>
        public SmartRoutingChatClient(
            IChatClientFactory chatClientFactory,
            ISmartRouter smartRouter,
            IHealthStore healthStore,
            IQuotaTracker quotaTracker,
            ResiliencePipelineProvider<string> pipelineProvider,
            IEnumerable<IChatClientStrategy> strategies,
            ActivitySource activitySource,
            IFallbackOrchestrator fallbackOrchestrator,
            ILogger<SmartRoutingChatClient> logger)
        {
            this.chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
            _ = smartRouter ?? throw new ArgumentNullException(nameof(smartRouter)); // Validate but don't store
            this.healthStore = healthStore ?? throw new ArgumentNullException(nameof(healthStore));
            this.quotaTracker = quotaTracker ?? throw new ArgumentNullException(nameof(quotaTracker));
            this.pipelineProvider = pipelineProvider ?? throw new ArgumentNullException(nameof(pipelineProvider));
            this.strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
            this.activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            this.fallbackOrchestrator = fallbackOrchestrator ?? throw new ArgumentNullException(nameof(fallbackOrchestrator));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the metadata for this chat client.
        /// </summary>
        public ChatClientMetadata Metadata { get; } = new("SmartRoutingChatClient");

        /// <summary>
        /// Gets a chat response asynchronously.
        /// </summary>
        /// <param name="messages">The chat messages.</param>
        /// <param name="options">The chat options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The chat response.</returns>
        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var activity = this.activitySource.StartActivity("ChatRequest");
            var modelId = options?.ModelId ?? "default";
            activity?.SetTag("model.id", modelId);

            string? preferredProviderKey = null;
            if (options?.AdditionalProperties?.TryGetValue("preferred_provider", out var preferred) == true)
            {
                preferredProviderKey = preferred?.ToString();
            }

            this.logger.LogInformation("Executing chat request with intelligent fallback for model '{ModelId}'", modelId);

            return await this.fallbackOrchestrator.ExecuteWithFallbackAsync(
                modelId,
                streaming: false,
                preferredProviderKey: preferredProviderKey,
                operation: async (candidate) => await this.ExecuteCandidateAsync(candidate, messages, options, cancellationToken).ConfigureAwait(false),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a streaming chat response asynchronously.
        /// </summary>
        /// <param name="messages">The chat messages.</param>
        /// <param name="options">The chat options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The chat response updates.</returns>
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var activity = this.activitySource.StartActivity("ChatRequest.Streaming");
            var modelId = options?.ModelId ?? "default";
            activity?.SetTag("model.id", modelId);

            string? preferredProviderKey = null;
            if (options?.AdditionalProperties?.TryGetValue("preferred_provider", out var preferred) == true)
            {
                preferredProviderKey = preferred?.ToString();
            }

            this.logger.LogInformation("Executing streaming chat request with intelligent fallback for model '{ModelId}'", modelId);

            var stream = await this.fallbackOrchestrator.ExecuteWithFallbackAsync(
                modelId,
                streaming: true,
                preferredProviderKey: preferredProviderKey,
                operation: async (candidate) => await this.ExecuteCandidateStreamingAsync(candidate, messages, options, cancellationToken).ConfigureAwait(false),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            await foreach (var update in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }
        }

        /// <summary>
        /// Gets a service from the factory.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <returns>The service instance.</returns>
        public object? GetService(Type serviceType, object? serviceKey = null) => this.chatClientFactory.GetService(serviceType, serviceKey);

        /// <summary>
        /// Disposes the chat client.
        /// </summary>
        public void Dispose()
        {
        }

        private static int? ExtractStatusFromAIException(Exception ex)
        {
            var exType = ex.GetType();
            if (!string.Equals(exType.Name, "AIException", StringComparison.Ordinal) && !string.Equals(exType.FullName, "Microsoft.Extensions.AI.AIException", StringComparison.Ordinal))
            {
                return null;
            }

            var statusProp = exType.GetProperties()
                .FirstOrDefault(p => (string.Equals(p.Name, "StatusCode", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(p.Name, "Status", StringComparison.OrdinalIgnoreCase))
                                     && (p.PropertyType == typeof(int)
                                         || p.PropertyType == typeof(System.Net.HttpStatusCode)));

            if (statusProp == null)
            {
                return null;
            }

            var val = statusProp.GetValue(ex);
            return val switch
            {
                System.Net.HttpStatusCode sc => (int)sc,
                int i => i,
                _ => null,
            };
        }

        private static int? ExtractStatusFromHttpException(Exception ex)
        {
            if (ex is not System.Net.Http.HttpRequestException httpEx)
            {
                return null;
            }

            var t = httpEx.GetType();
            if (t.GetProperties().Length == 0)
            {
                return null;
            }

            var p = t.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "StatusCode", StringComparison.OrdinalIgnoreCase)
                                                          && (p.PropertyType == typeof(System.Net.HttpStatusCode) || p.PropertyType == typeof(int)));

            if (p == null)
            {
                return null;
            }

            var val = p.GetValue(httpEx);
            return val switch
            {
                System.Net.HttpStatusCode sc => (int)sc,
                int i => i,
                _ => null,
            };
        }

        private static int? ExtractStatusFromInnerExceptions(Exception ex)
        {
            var inner = ex.InnerException;
            while (inner != null)
            {
                if (inner is System.Net.Http.HttpRequestException innerHttp)
                {
                    var statusCode = ExtractStatusFromHttpException(innerHttp);
                    if (statusCode.HasValue)
                    {
                        return statusCode;
                    }
                }
                else
                {
                    // Try to extract numeric status code from message (best-effort)
                    var m = StatusCodeRegex.Match(inner.Message ?? string.Empty);
                    if (m.Success && int.TryParse(m.Value, CultureInfo.InvariantCulture, out var parsed))
                    {
                        return parsed;
                    }
                }

                inner = inner.InnerException;
            }

            return null;
        }

        private static int? ExtractStatusCode(Exception ex)
        {
            try
            {
                // Check exception.Data for StatusCode (common pattern in tests)
                if (ex.Data["StatusCode"] is int dataCode)
                {
                    return dataCode;
                }

                // Check for AI exception types
                var statusCode = ExtractStatusFromAIException(ex);
                if (statusCode.HasValue)
                {
                    return statusCode;
                }

                // Check HttpRequestException
                statusCode = ExtractStatusFromHttpException(ex);
                if (statusCode.HasValue)
                {
                    return statusCode;
                }

                // Walk inner exceptions
                return ExtractStatusFromInnerExceptions(ex);
            }
            catch
            {
                // Best-effort inspection should not throw
                return null;
            }
        }

        private async Task<ChatResponse> ExecuteCandidateAsync(
            EnrichedCandidate candidate,
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? originalOptions,
            CancellationToken cancellationToken)
        {
            var client = this.chatClientFactory.GetClient(candidate.Key)
                         ?? throw new InvalidOperationException($"Provider '{candidate.Key}' not registered.");

            this.logger.LogInformation("Routing request to provider '{ProviderKey}'", candidate.Key);

            var routedOptions = originalOptions?.Clone() ?? new ChatOptions();
            routedOptions.ModelId = candidate.CanonicalModelPath;

            var pipeline = this.pipelineProvider.GetPipeline("provider-retry");
            var strategy = this.strategies.FirstOrDefault(s => s.CanHandle(candidate.Config.Type))
                           ?? this.strategies.FirstOrDefault() ?? throw new InvalidOperationException("No chat client strategies available");

            try
            {
                var response = await pipeline.ExecuteAsync(
                    async ct =>
                        await strategy.ExecuteAsync(client, chatMessages.ToList(), routedOptions, ct).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);

                // Add Routing Metadata
                if (response.AdditionalProperties == null)
                {
                    response.AdditionalProperties = new AdditionalPropertiesDictionary();
                }

                response.AdditionalProperties["provider_name"] = candidate.Key;
                response.AdditionalProperties["model_id"] = candidate.CanonicalModelPath;

                await this.RecordMetricsAsync(candidate.Key, response.Usage?.InputTokenCount ?? 0, response.Usage?.OutputTokenCount ?? 0, cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch (Exception ex)
            {
                await this.RecordFailureAsync(candidate.Key, candidate.CanonicalModelPath, ex, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        private async Task<IAsyncEnumerable<ChatResponseUpdate>> ExecuteCandidateStreamingAsync(
            EnrichedCandidate candidate,
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? originalOptions,
            CancellationToken cancellationToken)
        {
            var client = this.chatClientFactory.GetClient(candidate.Key)
                         ?? throw new InvalidOperationException($"Provider '{candidate.Key}' not registered.");

            this.logger.LogInformation("Routing streaming request to provider '{ProviderKey}'", candidate.Key);

            var routedOptions = originalOptions?.Clone() ?? new ChatOptions();
            routedOptions.ModelId = candidate.CanonicalModelPath;

            var pipeline = this.pipelineProvider.GetPipeline("provider-retry");
            var strategy = this.strategies.FirstOrDefault(s => s.CanHandle(candidate.Config.Type))
                           ?? this.strategies.FirstOrDefault() ?? throw new InvalidOperationException("No chat client strategies available");

            try
            {
                // We await the *creation* of the stream inside the resilience pipeline
                // If connection fails, pipeline retries. If stream starts, we return it.
                var stream = await pipeline.ExecuteAsync(
                    ct =>
                    {
                        var innerStream = strategy.ExecuteStreamingAsync(
                            client,
                            chatMessages.ToList(),
                            routedOptions,
                            ct);
                        return new ValueTask<IAsyncEnumerable<ChatResponseUpdate>>(innerStream);
                    },
                    cancellationToken).ConfigureAwait(false);

                // Wrap the stream to add metadata
                return AddMetadataToStream(stream, candidate.Key, candidate.CanonicalModelPath);
            }
            catch (Exception ex)
            {
                await this.RecordFailureAsync(candidate.Key, candidate.CanonicalModelPath, ex, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        private static async IAsyncEnumerable<ChatResponseUpdate> AddMetadataToStream(
            IAsyncEnumerable<ChatResponseUpdate> stream,
            string providerName,
            string modelId)
        {
            await foreach (var update in stream.ConfigureAwait(false))
            {
                // Add metadata to each update
                update.AdditionalProperties ??= new AdditionalPropertiesDictionary();
                update.AdditionalProperties["provider_name"] = providerName;
                update.AdditionalProperties["model_id"] = modelId;
                yield return update;
            }
        }

        private async Task RecordMetricsAsync(string providerKey, long inputTokens, long outputTokens, CancellationToken cancellationToken)
        {
            try
            {
                await this.healthStore.MarkSuccessAsync(providerKey, cancellationToken).ConfigureAwait(false);
                if (inputTokens > 0 || outputTokens > 0)
                {
                    await this.quotaTracker.RecordUsageAsync(providerKey, inputTokens, outputTokens, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to record metrics for provider '{ProviderKey}'.", providerKey);
            }
        }

        private async Task RecordFailureAsync(string providerKey, string? modelId, Exception ex, CancellationToken cancellationToken)
        {
            try
            {
                var statusCode = ExtractStatusCode(ex);
                var cooldown = this.DetermineCooldownAndLog(providerKey, modelId, ex, statusCode);

                if (cooldown.HasValue)
                {
                    await this.healthStore.MarkFailureAsync(providerKey, cooldown.Value, cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                // Suppress exceptions during failure recording to prevent cascading failures
            }
        }

        private TimeSpan? DetermineCooldownAndLog(string providerKey, string? modelId, Exception ex, int? statusCode)
        {
            if (!statusCode.HasValue)
            {
                // No status code found - fallback
                var cooldown = TimeSpan.FromSeconds(30);
                this.logger.LogError(ex, "Provider '{ProviderKey}' failed for model '{ModelId}' with no HTTP status code. Applying default cooldown {Cooldown}s.", providerKey, modelId ?? "unknown", cooldown.TotalSeconds);
                return cooldown;
            }

            var code = statusCode.Value;
            return code switch
            {
                429 => this.LogAndReturnCooldown(providerKey, modelId, ex, code, TimeSpan.FromSeconds(60), LogLevel.Warning, "returned 429 Too Many Requests"),
                401 => this.LogAndReturnCooldown(providerKey, modelId, ex, code, TimeSpan.FromHours(1), LogLevel.Critical, "returned 401 Unauthorized"),
                400 or 404 => this.LogClientError(providerKey, modelId, ex, code),
                >= 500 and < 600 => this.LogAndReturnCooldown(providerKey, modelId, ex, code, TimeSpan.FromSeconds(30), LogLevel.Error, "returned server error"),
                _ => this.LogAndReturnCooldown(providerKey, modelId, ex, code, TimeSpan.FromSeconds(30), LogLevel.Error, "returned unexpected status code"),
            };
        }

        private TimeSpan LogAndReturnCooldown(string providerKey, string? modelId, Exception ex, int statusCode, TimeSpan cooldown, LogLevel level, string message)
        {
            this.logger.Log(level, ex, "Provider '{ProviderKey}' {Message} for model '{ModelId}'. StatusCode: {StatusCode}. Applying cooldown {Cooldown}s.", providerKey, message, modelId ?? "unknown", statusCode, cooldown.TotalSeconds);
            return cooldown;
        }

        private TimeSpan? LogClientError(string providerKey, string? modelId, Exception ex, int statusCode)
        {
            // Do not penalize provider for model/input errors
            this.logger.LogError(ex, "Provider '{ProviderKey}' returned {StatusCode} (model/input error) for model '{ModelId}'. Not marking provider as failed.", providerKey, statusCode, modelId ?? "unknown");
            return null;
        }
    }
}

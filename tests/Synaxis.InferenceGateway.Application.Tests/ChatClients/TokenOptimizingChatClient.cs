using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.Tests.Optimization;
using Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;
using IRequestContextProvider = Synaxis.InferenceGateway.Application.Tests.Optimization.IRequestContextProvider;
using IRequestFingerprinter = Synaxis.InferenceGateway.Application.Tests.Optimization.IRequestFingerprinter;
using ISemanticCacheService = Synaxis.InferenceGateway.Application.Tests.Optimization.Caching.ISemanticCacheService;

namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

/// <summary>
/// Mock implementation of TokenOptimizingChatClient for testing
/// </summary>
public sealed class TokenOptimizingChatClient(
    IChatClient innerClient,
    ISemanticCacheService cacheService,
    IConversationStore conversationStore,
    ISessionStore sessionStore,
    IInFlightDeduplicationService deduplicationService,
    IRequestFingerprinter fingerprinter,
    ITokenOptimizationConfigurationResolver configResolver,
    IRequestContextProvider contextProvider,
    ILogger<TokenOptimizingChatClient> logger) : IChatClient
{
    private readonly IChatClient _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
    private readonly ISemanticCacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly IConversationStore _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
    private readonly ISessionStore _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    private readonly IInFlightDeduplicationService _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
    private readonly IRequestFingerprinter _fingerprinter = fingerprinter ?? throw new ArgumentNullException(nameof(fingerprinter));
    private readonly ITokenOptimizationConfigurationResolver _configResolver = configResolver ?? throw new ArgumentNullException(nameof(configResolver));
    private readonly IRequestContextProvider _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
    private readonly ILogger<TokenOptimizingChatClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // If optimization disabled, pass through
        if (!this._configResolver.IsOptimizationEnabled())
        {
            return await this._innerClient.GetResponseAsync(messages, options, cancellationToken);
        }

        var sessionId = this._fingerprinter.ComputeSessionId(this._contextProvider.GetCurrentContext()!);

        // Check for session affinity
        string? preferredProvider = null;
        if (this._configResolver.IsSessionAffinityEnabled())
        {
            preferredProvider = await this._sessionStore.GetPreferredProviderAsync(sessionId, cancellationToken);
            // Apply preferred provider to options if available
        }

        // Check cache
        if (this._configResolver.IsCachingEnabled())
        {
            var lastMessage = messages.Last();
            var cacheResult = await this._cacheService.TryGetCachedAsync(
                lastMessage.Text ?? "",
                sessionId,
                options?.ModelId ?? "default",
                options?.Temperature ?? 1.0,
                cancellationToken);

            if (cacheResult.IsHit)
            {
                var cachedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, cacheResult.Response!))
                {
                    AdditionalProperties = new AdditionalPropertiesDictionary
                    {
                        ["cache_hit"] = true
                    },
                };
                return cachedResponse;
            }
        }

        // Check for in-flight deduplication
        if (this._configResolver.IsDeduplicationEnabled())
        {
            var fingerprint = this._fingerprinter.ComputeFingerprint(messages, options);
            var inFlightResponse = await this._deduplicationService.TryGetInFlightAsync(fingerprint, cancellationToken);
            if (inFlightResponse != null)
            {
                inFlightResponse.AdditionalProperties = inFlightResponse.AdditionalProperties ?? new AdditionalPropertiesDictionary();
                inFlightResponse.AdditionalProperties["deduplicated"] = true;
                return inFlightResponse;
            }
        }

        // Apply compression if needed
        IEnumerable<ChatMessage> processedMessages = messages;
        if (this._configResolver.IsCompressionEnabled())
        {
            var messageList = messages.ToList();
            if (messageList.Count > this._configResolver.GetCompressionThreshold())
            {
                processedMessages = await this._conversationStore.CompressHistoryAsync(messages, cancellationToken);
            }
        }

        // Call inner client
        var response = await this._innerClient.GetResponseAsync(processedMessages, options, cancellationToken);

        // Add preferred provider to response if session affinity was used
        if (preferredProvider != null)
        {
            response.AdditionalProperties = response.AdditionalProperties ?? new AdditionalPropertiesDictionary();
            response.AdditionalProperties["preferred_provider"] = preferredProvider;
        }

        // Update conversation history
        await this._conversationStore.AddMessageAsync(sessionId, messages.Last(), cancellationToken);
        await this._conversationStore.AddMessageAsync(sessionId, response.Messages.First(), cancellationToken);

        // Update session affinity
        if (this._configResolver.IsSessionAffinityEnabled() && response.AdditionalProperties != null &&
            response.AdditionalProperties.TryGetValue("provider_name", out var providerName))
        {
            await this._sessionStore.SetPreferredProviderAsync(sessionId, providerName?.ToString() ?? "", cancellationToken);
        }

        // Cache the response
        if (this._configResolver.IsCachingEnabled())
        {
            var lastMessage = messages.Last();
            await this._cacheService.StoreAsync(
                lastMessage.Text ?? "",
                response.Messages.First().Text ?? "",
                sessionId,
                options?.ModelId ?? "default",
                options?.Temperature ?? 1.0,
                null,
                cancellationToken);
        }

        return response;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Streaming responses skip caching but apply session affinity
        var sessionId = this._fingerprinter.ComputeSessionId(this._contextProvider.GetCurrentContext()!);

        if (this._configResolver.IsOptimizationEnabled() && this._configResolver.IsSessionAffinityEnabled())
        {
            await this._sessionStore.GetPreferredProviderAsync(sessionId, cancellationToken);
        }

        await foreach (var update in this._innerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }

        // Update history after streaming completes
        if (this._configResolver.IsOptimizationEnabled())
        {
            await this._conversationStore.AddMessageAsync(sessionId, messages.Last(), cancellationToken);
        }
    }

    public ChatClientMetadata Metadata => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return this._innerClient.GetService(serviceType, serviceKey);
    }

    public void Dispose() => this._innerClient.Dispose();
}

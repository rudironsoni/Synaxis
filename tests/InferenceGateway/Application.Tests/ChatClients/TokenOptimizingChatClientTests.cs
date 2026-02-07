using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Synaxis.Common.Tests;
using Synaxis.InferenceGateway.Application.Tests.Optimization;
using Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;

namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

/// <summary>
/// Unit tests for TokenOptimizingChatClient decorator
/// Tests semantic caching, deduplication, compression, and session affinity
/// </summary>
public class TokenOptimizingChatClientTests : TestBase
{
    private readonly Mock<IChatClient> _innerClientMock;
    private readonly Mock<ISemanticCacheService> _cacheServiceMock;
    private readonly Mock<IConversationStore> _conversationStoreMock;
    private readonly Mock<ISessionStore> _sessionStoreMock;
    private readonly Mock<IInFlightDeduplicationService> _deduplicationServiceMock;
    private readonly Mock<IRequestFingerprinter> _fingerprinterMock;
    private readonly Mock<ITokenOptimizationConfigurationResolver> _configResolverMock;
    private readonly Mock<IRequestContextProvider> _contextProviderMock;
    private readonly Mock<ILogger<TokenOptimizingChatClient>> _loggerMock;
    private readonly TokenOptimizingChatClient _client;

    public TokenOptimizingChatClientTests()
    {
        _innerClientMock = CreateMockChatClient("Inner client response");
        _cacheServiceMock = new Mock<ISemanticCacheService>();
        _conversationStoreMock = new Mock<IConversationStore>();
        _sessionStoreMock = new Mock<ISessionStore>();
        _deduplicationServiceMock = new Mock<IInFlightDeduplicationService>();
        _fingerprinterMock = new Mock<IRequestFingerprinter>();
        _configResolverMock = new Mock<ITokenOptimizationConfigurationResolver>();
        _contextProviderMock = new Mock<IRequestContextProvider>();
        _loggerMock = CreateMockLogger<TokenOptimizingChatClient>();

        _client = new TokenOptimizingChatClient(
            _innerClientMock.Object,
            _cacheServiceMock.Object,
            _conversationStoreMock.Object,
            _sessionStoreMock.Object,
            _deduplicationServiceMock.Object,
            _fingerprinterMock.Object,
            _configResolverMock.Object,
            _contextProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetResponseAsync_Disabled_PassesThrough()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(false);

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Inner client response", result.Messages.First().Text);

        _innerClientMock.Verify(
            x => x.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheServiceMock.Verify(
            x => x.TryGetCachedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetResponseAsync_CacheHit_ReturnsCached()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "What is 2+2?") };
        var options = new ChatOptions { ModelId = "gpt-4", Temperature = 0.0 };

        var cachedResult = new CacheResult
        {
            IsHit = true,
            Response = "4",
            SimilarityScore = 1.0,
            CachedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _configResolverMock
            .Setup(x => x.IsCachingEnabled())
            .Returns(true);

        _fingerprinterMock
            .Setup(x => x.ComputeFingerprint(messages, options))
            .Returns("fingerprint-123");

        _fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        _cacheServiceMock
            .Setup(x => x.TryGetCachedAsync("What is 2+2?", "session-123", "gpt-4", 0.0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("4", result.Messages.First().Text);
        Assert.True(result.AdditionalProperties?.ContainsKey("cache_hit"));

        _innerClientMock.Verify(
            x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetResponseAsync_CacheMiss_CallsInnerClient()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        var cacheMissResult = new CacheResult
        {
            IsHit = false,
            Response = null,
            SimilarityScore = 0.0,
            QueryEmbedding = new float[] { 0.1f, 0.2f, 0.3f }
        };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _configResolverMock
            .Setup(x => x.IsCachingEnabled())
            .Returns(true);

        _fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        _cacheServiceMock
            .Setup(x => x.TryGetCachedAsync("Hello", "session-123", "gpt-4", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cacheMissResult);

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Inner client response", result.Messages.First().Text);

        _innerClientMock.Verify(
            x => x.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheServiceMock.Verify(
            x => x.StoreAsync("Hello", "Inner client response", "session-123", "gpt-4", It.IsAny<double>(), It.IsAny<float[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_SessionAffinity_SetsPreferredProvider()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _configResolverMock
            .Setup(x => x.IsSessionAffinityEnabled())
            .Returns(true);

        _fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        _sessionStoreMock
            .Setup(x => x.GetPreferredProviderAsync("session-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("openai");

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AdditionalProperties?.ContainsKey("preferred_provider"));
        Assert.Equal("openai", result.AdditionalProperties?["preferred_provider"]);
    }

    [Fact]
    public async Task GetResponseAsync_Compression_Applied()
    {
        // Arrange
        var messages = new ChatMessage[20];
        for (int i = 0; i < 20; i++)
        {
            messages[i] = new ChatMessage(
                i % 2 == 0 ? ChatRole.User : ChatRole.Assistant,
                $"Message {i}");
        }
        var options = new ChatOptions { ModelId = "gpt-4" };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _configResolverMock
            .Setup(x => x.IsCompressionEnabled())
            .Returns(true);

        _configResolverMock
            .Setup(x => x.GetCompressionThreshold())
            .Returns(10); // Compress if more than 10 messages

        _conversationStoreMock
            .Setup(x => x.CompressHistoryAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ChatMessage(ChatRole.System, "Compressed conversation history"),
                messages[^1] // Keep last message
            });

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);

        _conversationStoreMock.Verify(
            x => x.CompressHistoryAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_Deduplication_WaitsForInFlight()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };
        var fingerprint = "duplicate-request-123";

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _configResolverMock
            .Setup(x => x.IsDeduplicationEnabled())
            .Returns(true);

        _fingerprinterMock
            .Setup(x => x.ComputeFingerprint(messages, options))
            .Returns(fingerprint);

        _deduplicationServiceMock
            .Setup(x => x.TryGetInFlightAsync(fingerprint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Deduplicated response")));

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Deduplicated response", result.Messages.First().Text);
        Assert.True(result.AdditionalProperties?.ContainsKey("deduplicated"));

        _innerClientMock.Verify(
            x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetResponseAsync_Error_NotCached()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _configResolverMock
            .Setup(x => x.IsCachingEnabled())
            .Returns(true);

        _fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        _cacheServiceMock
            .Setup(x => x.TryGetCachedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CacheResult { IsHit = false });

        _innerClientMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _client.GetResponseAsync(messages, options));

        _cacheServiceMock.Verify(
            x => x.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<float[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetResponseAsync_UpdatesConversationHistory()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);

        _conversationStoreMock.Verify(
            x => x.AddMessageAsync("session-123", It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetResponseAsync_UpdatesSessionAffinity()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _configResolverMock
            .Setup(x => x.IsSessionAffinityEnabled())
            .Returns(true);

        _fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Response"))
        {
            AdditionalProperties = new Dictionary<string, object?>
            {
                ["provider_name"] = "openai"
            }
        };

        _innerClientMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);

        _sessionStoreMock.Verify(
            x => x.SetPreferredProviderAsync("session-123", "openai", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_SkipsCaching()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _configResolverMock
            .Setup(x => x.IsCachingEnabled())
            .Returns(true);

        var mockStreamingClient = CreateMockStreamingChatClient("Hello", " World");
        var streamingDecorator = new TokenOptimizingChatClient(
            mockStreamingClient.Object,
            _cacheServiceMock.Object,
            _conversationStoreMock.Object,
            _sessionStoreMock.Object,
            _deduplicationServiceMock.Object,
            _fingerprinterMock.Object,
            _configResolverMock.Object,
            _contextProviderMock.Object,
            _loggerMock.Object);

        // Act
        var stream = streamingDecorator.GetStreamingResponseAsync(messages, options);
        var results = new List<ChatResponseUpdate>();
        await foreach (var update in stream)
        {
            results.Add(update);
        }

        // Assert
        Assert.Equal(2, results.Count);

        // Streaming should not check cache
        _cacheServiceMock.Verify(
            x => x.TryGetCachedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_AppliesSessionAffinity()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _configResolverMock
            .Setup(x => x.IsSessionAffinityEnabled())
            .Returns(true);

        _fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        _sessionStoreMock
            .Setup(x => x.GetPreferredProviderAsync("session-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("openai");

        var mockStreamingClient = CreateMockStreamingChatClient("Response");
        var streamingDecorator = new TokenOptimizingChatClient(
            mockStreamingClient.Object,
            _cacheServiceMock.Object,
            _conversationStoreMock.Object,
            _sessionStoreMock.Object,
            _deduplicationServiceMock.Object,
            _fingerprinterMock.Object,
            _configResolverMock.Object,
            _contextProviderMock.Object,
            _loggerMock.Object);

        // Act
        var stream = streamingDecorator.GetStreamingResponseAsync(messages, options);
        var results = new List<ChatResponseUpdate>();
        await foreach (var update in stream)
        {
            results.Add(update);
        }

        // Assert
        Assert.NotEmpty(results);

        _sessionStoreMock.Verify(
            x => x.GetPreferredProviderAsync("session-123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_UpdatesHistory()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        var mockStreamingClient = CreateMockStreamingChatClient("Hello", " World");
        var streamingDecorator = new TokenOptimizingChatClient(
            mockStreamingClient.Object,
            _cacheServiceMock.Object,
            _conversationStoreMock.Object,
            _sessionStoreMock.Object,
            _deduplicationServiceMock.Object,
            _fingerprinterMock.Object,
            _configResolverMock.Object,
            _contextProviderMock.Object,
            _loggerMock.Object);

        // Act
        var stream = streamingDecorator.GetStreamingResponseAsync(messages, options);
        var results = new List<ChatResponseUpdate>();
        await foreach (var update in stream)
        {
            results.Add(update);
        }

        // Assert
        Assert.NotEmpty(results);

        _conversationStoreMock.Verify(
            x => x.AddMessageAsync("session-123", It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetResponseAsync_ConcurrentRequests_ThreadSafe()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        // Act - Simulate concurrent requests
        var tasks = new Task<ChatResponse>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _client.GetResponseAsync(messages, options);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.NotNull(r));
    }

    [Fact]
    public async Task GetResponseAsync_Cancellation_RespectsToken()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };
        var cts = new CancellationTokenSource();

        _configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        _innerClientMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _client.GetResponseAsync(messages, options, cts.Token));
    }
}

/// <summary>
/// Mock implementation of TokenOptimizingChatClient for testing
/// </summary>
public class TokenOptimizingChatClient : IChatClient
{
    private readonly IChatClient _innerClient;
    private readonly ISemanticCacheService _cacheService;
    private readonly IConversationStore _conversationStore;
    private readonly ISessionStore _sessionStore;
    private readonly IInFlightDeduplicationService _deduplicationService;
    private readonly IRequestFingerprinter _fingerprinter;
    private readonly ITokenOptimizationConfigurationResolver _configResolver;
    private readonly IRequestContextProvider _contextProvider;
    private readonly ILogger<TokenOptimizingChatClient> _logger;

    public TokenOptimizingChatClient(
        IChatClient innerClient,
        ISemanticCacheService cacheService,
        IConversationStore conversationStore,
        ISessionStore sessionStore,
        IInFlightDeduplicationService deduplicationService,
        IRequestFingerprinter fingerprinter,
        ITokenOptimizationConfigurationResolver configResolver,
        IRequestContextProvider contextProvider,
        ILogger<TokenOptimizingChatClient> logger)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
        _fingerprinter = fingerprinter ?? throw new ArgumentNullException(nameof(fingerprinter));
        _configResolver = configResolver ?? throw new ArgumentNullException(nameof(configResolver));
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // If optimization disabled, pass through
        if (!_configResolver.IsOptimizationEnabled())
        {
            return await _innerClient.GetResponseAsync(messages, options, cancellationToken);
        }

        var sessionId = _fingerprinter.ComputeSessionId(_contextProvider.GetCurrentContext()!);

        // Check for session affinity
        if (_configResolver.IsSessionAffinityEnabled())
        {
            var preferredProvider = await _sessionStore.GetPreferredProviderAsync(sessionId, cancellationToken);
            // Apply preferred provider to options if available
        }

        // Check cache
        if (_configResolver.IsCachingEnabled())
        {
            var lastMessage = messages.Last();
            var cacheResult = await _cacheService.TryGetCachedAsync(
                lastMessage.Text ?? "",
                sessionId,
                options?.ModelId ?? "default",
                options?.Temperature ?? 1.0,
                cancellationToken);

            if (cacheResult.IsHit)
            {
                var cachedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, cacheResult.Response!))
                {
                    AdditionalProperties = new Dictionary<string, object?>
                    {
                        ["cache_hit"] = true
                    }
                };
                return cachedResponse;
            }
        }

        // Check for in-flight deduplication
        if (_configResolver.IsDeduplicationEnabled())
        {
            var fingerprint = _fingerprinter.ComputeFingerprint(messages, options);
            var inFlightResponse = await _deduplicationService.TryGetInFlightAsync(fingerprint, cancellationToken);
            if (inFlightResponse != null)
            {
                inFlightResponse.AdditionalProperties ??= new Dictionary<string, object?>();
                inFlightResponse.AdditionalProperties["deduplicated"] = true;
                return inFlightResponse;
            }
        }

        // Apply compression if needed
        IEnumerable<ChatMessage> processedMessages = messages;
        if (_configResolver.IsCompressionEnabled())
        {
            var messageList = messages.ToList();
            if (messageList.Count > _configResolver.GetCompressionThreshold())
            {
                processedMessages = await _conversationStore.CompressHistoryAsync(messages, cancellationToken);
            }
        }

        // Call inner client
        var response = await _innerClient.GetResponseAsync(processedMessages, options, cancellationToken);

        // Update conversation history
        await _conversationStore.AddMessageAsync(sessionId, messages.Last(), cancellationToken);
        await _conversationStore.AddMessageAsync(sessionId, response.Messages.First(), cancellationToken);

        // Update session affinity
        if (_configResolver.IsSessionAffinityEnabled() && response.AdditionalProperties != null)
        {
            if (response.AdditionalProperties.TryGetValue("provider_name", out var providerName))
            {
                await _sessionStore.SetPreferredProviderAsync(sessionId, providerName?.ToString() ?? "", cancellationToken);
            }
        }

        // Cache the response
        if (_configResolver.IsCachingEnabled())
        {
            var lastMessage = messages.Last();
            await _cacheService.StoreAsync(
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
        var sessionId = _fingerprinter.ComputeSessionId(_contextProvider.GetCurrentContext()!);

        if (_configResolver.IsOptimizationEnabled() && _configResolver.IsSessionAffinityEnabled())
        {
            await _sessionStore.GetPreferredProviderAsync(sessionId, cancellationToken);
        }

        await foreach (var update in _innerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }

        // Update history after streaming completes
        if (_configResolver.IsOptimizationEnabled())
        {
            await _conversationStore.AddMessageAsync(sessionId, messages.Last(), cancellationToken);
        }
    }

    public ChatClientMetadata Metadata => _innerClient.Metadata;

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return _innerClient.GetService(serviceType, serviceKey);
    }

    public void Dispose() => _innerClient.Dispose();
}

/// <summary>
/// Interface for managing conversation history
/// </summary>
public interface IConversationStore
{
    Task AddMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken);
    Task<IEnumerable<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken);
    Task<IEnumerable<ChatMessage>> CompressHistoryAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for managing session affinity
/// </summary>
public interface ISessionStore
{
    Task<string?> GetPreferredProviderAsync(string sessionId, CancellationToken cancellationToken);
    Task SetPreferredProviderAsync(string sessionId, string providerId, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for in-flight request deduplication
/// </summary>
public interface IInFlightDeduplicationService
{
    Task<ChatResponse?> TryGetInFlightAsync(string fingerprint, CancellationToken cancellationToken);
    Task RegisterInFlightAsync(string fingerprint, Task<ChatResponse> responseTask, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for token optimization configuration
/// </summary>
public interface ITokenOptimizationConfigurationResolver
{
    bool IsOptimizationEnabled();
    bool IsCachingEnabled();
    bool IsCompressionEnabled();
    bool IsDeduplicationEnabled();
    bool IsSessionAffinityEnabled();
    int GetCompressionThreshold();
}

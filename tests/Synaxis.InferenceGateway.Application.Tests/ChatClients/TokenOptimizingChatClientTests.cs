using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.Common.Tests;
using Synaxis.InferenceGateway.Application.Tests.Optimization;
using Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;
using Xunit;
using IRequestContextProvider = Synaxis.InferenceGateway.Application.Tests.Optimization.IRequestContextProvider;
using IRequestFingerprinter = Synaxis.InferenceGateway.Application.Tests.Optimization.IRequestFingerprinter;
using ISemanticCacheService = Synaxis.InferenceGateway.Application.Tests.Optimization.Caching.ISemanticCacheService;

namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

/// <summary>
/// Unit tests for TokenOptimizingChatClient decorator
/// Tests semantic caching, deduplication, compression, and session affinity
/// </summary>
public sealed class TokenOptimizingChatClientTests : TestBase, IDisposable
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
        this._innerClientMock = TestBase.CreateMockChatClient("Inner client response");
        this._cacheServiceMock = new Mock<ISemanticCacheService>();
        this._conversationStoreMock = new Mock<IConversationStore>();
        this._sessionStoreMock = new Mock<ISessionStore>();
        this._deduplicationServiceMock = new Mock<IInFlightDeduplicationService>();
        this._fingerprinterMock = new Mock<IRequestFingerprinter>();
        this._configResolverMock = new Mock<ITokenOptimizationConfigurationResolver>();
        this._contextProviderMock = new Mock<IRequestContextProvider>();
        this._loggerMock = TestBase.CreateMockLogger<TokenOptimizingChatClient>();

        this._client = new TokenOptimizingChatClient(
            this._innerClientMock.Object,
            this._cacheServiceMock.Object,
            this._conversationStoreMock.Object,
            this._sessionStoreMock.Object,
            this._deduplicationServiceMock.Object,
            this._fingerprinterMock.Object,
            this._configResolverMock.Object,
            this._contextProviderMock.Object,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task GetResponseAsync_Disabled_PassesThrough()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(false);

        // Act
        var result = await this._client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Inner client response", result.Messages.First().Text);

        this._innerClientMock.Verify(
            x => x.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()),
            Times.Once);

        this._cacheServiceMock.Verify(
            x => x.TryGetCachedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetResponseAsync_CacheHit_ReturnsCached()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "What is 2+2?") };
        var options = new ChatOptions { ModelId = "gpt-4", Temperature = (float?)0.0 };

        var cachedResult = new CacheResult
        {
            IsHit = true,
            Response = "4",
            SimilarityScore = 1.0,
            CachedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._configResolverMock
            .Setup(x => x.IsCachingEnabled())
            .Returns(true);

        this._fingerprinterMock
            .Setup(x => x.ComputeFingerprint(messages, options))
            .Returns("fingerprint-123");

        this._fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        this._cacheServiceMock
            .Setup(x => x.TryGetCachedAsync("What is 2+2?", "session-123", "gpt-4", 0.0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        // Act
        var result = await this._client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("4", result.Messages.First().Text);
        Assert.True(result.AdditionalProperties?.ContainsKey("cache_hit"));

        this._innerClientMock.Verify(
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
            QueryEmbedding = new float[] { 0.1f, 0.2f, 0.3f },
        };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._configResolverMock
            .Setup(x => x.IsCachingEnabled())
            .Returns(true);

        this._fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        this._cacheServiceMock
            .Setup(x => x.TryGetCachedAsync("Hello", "session-123", "gpt-4", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cacheMissResult);

        // Act
        var result = await this._client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Inner client response", result.Messages.First().Text);

        this._innerClientMock.Verify(
            x => x.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()),
            Times.Once);

        this._cacheServiceMock.Verify(
            x => x.StoreAsync("Hello", "Inner client response", "session-123", "gpt-4", It.IsAny<double>(), It.IsAny<float[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_SessionAffinity_SetsPreferredProvider()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._configResolverMock
            .Setup(x => x.IsSessionAffinityEnabled())
            .Returns(true);

        this._fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        this._sessionStoreMock
            .Setup(x => x.GetPreferredProviderAsync("session-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("openai");

        // Act
        var result = await this._client.GetResponseAsync(messages, options);

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

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._configResolverMock
            .Setup(x => x.IsCompressionEnabled())
            .Returns(true);

        this._configResolverMock
            .Setup(x => x.GetCompressionThreshold())
            .Returns(10); // Compress if more than 10 messages

        this._conversationStoreMock
            .Setup(x => x.CompressHistoryAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ChatMessage(ChatRole.System, "Compressed conversation history"),
                messages[^1], // Keep last message
            });

        // Act
        var result = await this._client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);

        this._conversationStoreMock.Verify(
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

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._configResolverMock
            .Setup(x => x.IsDeduplicationEnabled())
            .Returns(true);

        this._fingerprinterMock
            .Setup(x => x.ComputeFingerprint(messages, options))
            .Returns(fingerprint);

        this._deduplicationServiceMock
            .Setup(x => x.TryGetInFlightAsync(fingerprint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Deduplicated response")));

        // Act
        var result = await this._client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Deduplicated response", result.Messages.First().Text);
        Assert.True(result.AdditionalProperties?.ContainsKey("deduplicated"));

        this._innerClientMock.Verify(
            x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetResponseAsync_Error_NotCached()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._configResolverMock
            .Setup(x => x.IsCachingEnabled())
            .Returns(true);

        this._fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        this._cacheServiceMock
            .Setup(x => x.TryGetCachedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CacheResult { IsHit = false });

        this._innerClientMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await this._client.GetResponseAsync(messages, options));

        this._cacheServiceMock.Verify(
            x => x.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<float[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetResponseAsync_UpdatesConversationHistory()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        // Act
        var result = await this._client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);

        this._conversationStoreMock.Verify(
            x => x.AddMessageAsync("session-123", It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetResponseAsync_UpdatesSessionAffinity()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._configResolverMock
            .Setup(x => x.IsSessionAffinityEnabled())
            .Returns(true);

        this._fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Response"))
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["provider_name"] = "openai"
            },
        };

        this._innerClientMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await this._client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);

        this._sessionStoreMock.Verify(
            x => x.SetPreferredProviderAsync("session-123", "openai", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_SkipsCaching()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._configResolverMock
            .Setup(x => x.IsCachingEnabled())
            .Returns(true);

        var mockStreamingClient = TestBase.CreateMockStreamingChatClient("Hello", " World");
        var streamingDecorator = new TokenOptimizingChatClient(
            mockStreamingClient.Object,
            this._cacheServiceMock.Object,
            this._conversationStoreMock.Object,
            this._sessionStoreMock.Object,
            this._deduplicationServiceMock.Object,
            this._fingerprinterMock.Object,
            this._configResolverMock.Object,
            this._contextProviderMock.Object,
            this._loggerMock.Object);

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
        this._cacheServiceMock.Verify(
            x => x.TryGetCachedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_AppliesSessionAffinity()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._configResolverMock
            .Setup(x => x.IsSessionAffinityEnabled())
            .Returns(true);

        this._fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        this._sessionStoreMock
            .Setup(x => x.GetPreferredProviderAsync("session-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("openai");

        var mockStreamingClient = TestBase.CreateMockStreamingChatClient("Response");
        var streamingDecorator = new TokenOptimizingChatClient(
            mockStreamingClient.Object,
            this._cacheServiceMock.Object,
            this._conversationStoreMock.Object,
            this._sessionStoreMock.Object,
            this._deduplicationServiceMock.Object,
            this._fingerprinterMock.Object,
            this._configResolverMock.Object,
            this._contextProviderMock.Object,
            this._loggerMock.Object);

        // Act
        var stream = streamingDecorator.GetStreamingResponseAsync(messages, options);
        var results = new List<ChatResponseUpdate>();
        await foreach (var update in stream)
        {
            results.Add(update);
        }

        // Assert
        Assert.NotEmpty(results);

        this._sessionStoreMock.Verify(
            x => x.GetPreferredProviderAsync("session-123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_UpdatesHistory()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        var mockStreamingClient = TestBase.CreateMockStreamingChatClient("Hello", " World");
        var streamingDecorator = new TokenOptimizingChatClient(
            mockStreamingClient.Object,
            this._cacheServiceMock.Object,
            this._conversationStoreMock.Object,
            this._sessionStoreMock.Object,
            this._deduplicationServiceMock.Object,
            this._fingerprinterMock.Object,
            this._configResolverMock.Object,
            this._contextProviderMock.Object,
            this._loggerMock.Object);

        // Act
        var stream = streamingDecorator.GetStreamingResponseAsync(messages, options);
        var results = new List<ChatResponseUpdate>();
        await foreach (var update in stream)
        {
            results.Add(update);
        }

        // Assert
        Assert.NotEmpty(results);

        this._conversationStoreMock.Verify(
            x => x.AddMessageAsync("session-123", It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetResponseAsync_ConcurrentRequests_ThreadSafe()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._fingerprinterMock
            .Setup(x => x.ComputeSessionId(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
            .Returns("session-123");

        // Act - Simulate concurrent requests
        var tasks = new Task<ChatResponse>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = this._client.GetResponseAsync(messages, options);
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
        using var cts = new CancellationTokenSource();

        this._configResolverMock
            .Setup(x => x.IsOptimizationEnabled())
            .Returns(true);

        this._innerClientMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await this._client.GetResponseAsync(messages, options, cts.Token));
    }

    public void Dispose()
    {
        this._client?.Dispose();
    }
}

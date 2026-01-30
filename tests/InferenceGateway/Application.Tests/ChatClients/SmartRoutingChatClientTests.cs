using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.Retry;
using Polly.Registry;
using Synaxis.Common.Tests;
using Synaxis.InferenceGateway.Application.ChatClients;
using Synaxis.InferenceGateway.Application.ChatClients.Strategies;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.Routing;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

public class SmartRoutingChatClientTests : TestBase
{
    private readonly Mock<IChatClientFactory> _chatClientFactoryMock;
    private readonly Mock<ISmartRouter> _smartRouterMock;
    private readonly Mock<IHealthStore> _healthStoreMock;
    private readonly Mock<IQuotaTracker> _quotaTrackerMock;
    private readonly Mock<ResiliencePipelineProvider<string>> _pipelineProviderMock;
    private readonly Mock<IEnumerable<IChatClientStrategy>> _strategiesMock;
    private readonly ActivitySource _activitySource;
    private readonly Mock<ILogger<SmartRoutingChatClient>> _loggerMock;
    private readonly SmartRoutingChatClient _client;

    public SmartRoutingChatClientTests()
    {
        _chatClientFactoryMock = new Mock<IChatClientFactory>();
        _smartRouterMock = new Mock<ISmartRouter>();
        _healthStoreMock = new Mock<IHealthStore>();
        _quotaTrackerMock = new Mock<IQuotaTracker>();
        _pipelineProviderMock = new Mock<ResiliencePipelineProvider<string>>();
        _strategiesMock = new Mock<IEnumerable<IChatClientStrategy>>();
        _activitySource = new ActivitySource("Test");
        _loggerMock = CreateMockLogger<SmartRoutingChatClient>();

        // Setup pipeline provider to return a pipeline with retry support
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffType = DelayBackoffType.Constant
            })
            .Build();
        _pipelineProviderMock.Setup(x => x.GetPipeline("provider-retry"))
            .Returns(pipeline);

        _client = new SmartRoutingChatClient(
            _chatClientFactoryMock.Object,
            _smartRouterMock.Object,
            _healthStoreMock.Object,
            _quotaTrackerMock.Object,
            _pipelineProviderMock.Object,
            _strategiesMock.Object,
            _activitySource,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetResponseAsync_SingleProvider_Success()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };
        var candidate = new EnrichedCandidate(
            new ProviderConfig { Key = "openai", Type = "openai" },
            null,
            "gpt-4");

        var mockChatClient = CreateMockChatClient();
        var mockStrategy = new Mock<IChatClientStrategy>();

        _smartRouterMock.Setup(x => x.GetCandidatesAsync("gpt-4", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrichedCandidate> { candidate });
        _quotaTrackerMock.Setup(x => x.IsHealthyAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _chatClientFactoryMock.Setup(x => x.GetClient("openai"))
            .Returns(mockChatClient.Object);
        _strategiesMock.Setup(x => x.GetEnumerator())
            .Returns(new List<IChatClientStrategy> { mockStrategy.Object }.GetEnumerator());
        mockStrategy.Setup(x => x.CanHandle("openai"))
            .Returns(true);
        mockStrategy.Setup(x => x.ExecuteAsync(mockChatClient.Object, It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello there!")));

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello there!", result.Messages.First().Text);
        Assert.Equal("openai", result.AdditionalProperties?["provider_name"]);
        Assert.Equal("gpt-4", result.AdditionalProperties?["model_id"]);
    }

    [Fact]
    public async Task GetResponseAsync_TieredFailover_FallsBackToTier2()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };
        var candidate1 = new EnrichedCandidate(
            new ProviderConfig { Key = "openai", Type = "openai" },
            null,
            "gpt-4");
        var candidate2 = new EnrichedCandidate(
            new ProviderConfig { Key = "groq", Type = "groq" },
            null,
            "llama-3.1-70b-versatile");

        var mockChatClient1 = CreateMockChatClient();
        var mockChatClient2 = CreateMockChatClient("Fallback response");
        var mockStrategy = new Mock<IChatClientStrategy>();

        _smartRouterMock.Setup(x => x.GetCandidatesAsync("gpt-4", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrichedCandidate> { candidate1, candidate2 });
        _quotaTrackerMock.Setup(x => x.IsHealthyAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _quotaTrackerMock.Setup(x => x.IsHealthyAsync("groq", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _chatClientFactoryMock.Setup(x => x.GetClient("openai"))
            .Returns(mockChatClient1.Object);
        _chatClientFactoryMock.Setup(x => x.GetClient("groq"))
            .Returns(mockChatClient2.Object);
        _strategiesMock.Setup(x => x.GetEnumerator())
            .Returns(new List<IChatClientStrategy> { mockStrategy.Object }.GetEnumerator());
        mockStrategy.Setup(x => x.CanHandle(It.IsAny<string>()))
            .Returns(true);
        mockStrategy.Setup(x => x.ExecuteAsync(mockChatClient1.Object, It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider failed"));
        mockStrategy.Setup(x => x.ExecuteAsync(mockChatClient2.Object, It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Fallback response")));

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Fallback response", result.Messages.First().Text);
        Assert.Equal("groq", result.AdditionalProperties?["provider_name"]);
    }

    [Fact]
    public async Task GetResponseAsync_WithCancellation_ThrowsAggregateException()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };
        var candidate = new EnrichedCandidate(
            new ProviderConfig { Key = "openai", Type = "openai" },
            null,
            "gpt-4");

        var mockChatClient = CreateMockChatClient();
        var mockStrategy = new Mock<IChatClientStrategy>();

        _smartRouterMock.Setup(x => x.GetCandidatesAsync("gpt-4", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrichedCandidate> { candidate });
        _quotaTrackerMock.Setup(x => x.IsHealthyAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _chatClientFactoryMock.Setup(x => x.GetClient("openai"))
            .Returns(mockChatClient.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
            await _client.GetResponseAsync(messages, options, cts.Token));
        
        Assert.Contains("All providers failed for model 'gpt-4'", exception.Message);
    }

    [Fact]
    public async Task GetResponseAsync_RecordsSuccess()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };
        var candidate = new EnrichedCandidate(
            new ProviderConfig { Key = "openai", Type = "openai" },
            null,
            "gpt-4");

        var mockChatClient = CreateMockChatClient();
        var mockStrategy = new Mock<IChatClientStrategy>();

        _smartRouterMock.Setup(x => x.GetCandidatesAsync("gpt-4", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrichedCandidate> { candidate });
        _quotaTrackerMock.Setup(x => x.IsHealthyAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _chatClientFactoryMock.Setup(x => x.GetClient("openai"))
            .Returns(mockChatClient.Object);

        // Create a real list instead of mocking IEnumerable
        var strategiesList = new List<IChatClientStrategy> { mockStrategy.Object };

        // Create a new client with the real list
        var clientWithStrategy = new SmartRoutingChatClient(
            _chatClientFactoryMock.Object,
            _smartRouterMock.Object,
            _healthStoreMock.Object,
            _quotaTrackerMock.Object,
            _pipelineProviderMock.Object,
            strategiesList,
            _activitySource,
            _loggerMock.Object);

        mockStrategy.Setup(x => x.CanHandle("openai"))
            .Returns(true);
        mockStrategy.Setup(x => x.ExecuteAsync(mockChatClient.Object, It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello there!"))
            {
                Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 20 }
            });

        // Act
        await clientWithStrategy.GetResponseAsync(messages, options);

        // Assert
        _healthStoreMock.Verify(x => x.MarkSuccessAsync("openai", It.IsAny<CancellationToken>()), Times.Once);
        _quotaTrackerMock.Verify(x => x.RecordUsageAsync("openai", It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_RecordsFailure()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };
        var candidate = new EnrichedCandidate(
            new ProviderConfig { Key = "openai", Type = "openai" },
            null,
            "gpt-4");

        var mockChatClient = CreateMockChatClient();
        var mockStrategy = new Mock<IChatClientStrategy>();

        _smartRouterMock.Setup(x => x.GetCandidatesAsync("gpt-4", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrichedCandidate> { candidate });
        _quotaTrackerMock.Setup(x => x.IsHealthyAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _chatClientFactoryMock.Setup(x => x.GetClient("openai"))
            .Returns(mockChatClient.Object);
        _strategiesMock.Setup(x => x.GetEnumerator())
            .Returns(new List<IChatClientStrategy> { mockStrategy.Object }.GetEnumerator());
        mockStrategy.Setup(x => x.CanHandle("openai"))
            .Returns(true);
        mockStrategy.Setup(x => x.ExecuteAsync(mockChatClient.Object, It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider failed"));

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => _client.GetResponseAsync(messages, options));
        _healthStoreMock.Verify(x => x.MarkFailureAsync("openai", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_SingleProvider_Success()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };
        var candidate = new EnrichedCandidate(
            new ProviderConfig { Key = "openai", Type = "openai" },
            null,
            "gpt-4");

        var mockChatClient = CreateMockStreamingChatClient("Hello", " there!");
        var mockStrategy = new Mock<IChatClientStrategy>();

        _smartRouterMock.Setup(x => x.GetCandidatesAsync("gpt-4", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrichedCandidate> { candidate });
        _quotaTrackerMock.Setup(x => x.IsHealthyAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _chatClientFactoryMock.Setup(x => x.GetClient("openai"))
            .Returns(mockChatClient.Object);
        _strategiesMock.Setup(x => x.GetEnumerator())
            .Returns(new List<IChatClientStrategy> { mockStrategy.Object }.GetEnumerator());
        mockStrategy.Setup(x => x.CanHandle("openai"))
            .Returns(true);
        mockStrategy.Setup(x => x.ExecuteStreamingAsync(mockChatClient.Object, It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Returns(GenerateStreamingResponse(new[] { "Hello", " there!" }));

        // Act
        var stream = _client.GetStreamingResponseAsync(messages, options);
        var results = new List<ChatResponseUpdate>();
        await foreach (var update in stream)
        {
            results.Add(update);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Hello", results[0].Text);
        Assert.Equal(" there!", results[1].Text);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WithFailover_SwitchesProvider()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };
        var candidate1 = new EnrichedCandidate(
            new ProviderConfig { Key = "openai", Type = "openai" },
            null,
            "gpt-4");
        var candidate2 = new EnrichedCandidate(
            new ProviderConfig { Key = "groq", Type = "groq" },
            null,
            "llama-3.1-70b-versatile");

        var mockChatClient1 = CreateMockStreamingChatClient();
        var mockChatClient2 = CreateMockStreamingChatClient("Fallback", " response");
        var mockStrategy = new Mock<IChatClientStrategy>();

        _smartRouterMock.Setup(x => x.GetCandidatesAsync("gpt-4", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrichedCandidate> { candidate1, candidate2 });
        _quotaTrackerMock.Setup(x => x.IsHealthyAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _quotaTrackerMock.Setup(x => x.IsHealthyAsync("groq", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _chatClientFactoryMock.Setup(x => x.GetClient("openai"))
            .Returns(mockChatClient1.Object);
        _chatClientFactoryMock.Setup(x => x.GetClient("groq"))
            .Returns(mockChatClient2.Object);
        _strategiesMock.Setup(x => x.GetEnumerator())
            .Returns(new List<IChatClientStrategy> { mockStrategy.Object }.GetEnumerator());
        mockStrategy.Setup(x => x.CanHandle(It.IsAny<string>()))
            .Returns(true);
        mockStrategy.Setup(x => x.ExecuteStreamingAsync(mockChatClient1.Object, It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Returns(ThrowingStream(new Exception("Provider failed")));
        mockStrategy.Setup(x => x.ExecuteStreamingAsync(mockChatClient2.Object, It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Returns(GenerateStreamingResponse(new[] { "Fallback", " response" }));

        // Act
        var stream = _client.GetStreamingResponseAsync(messages, options);
        var results = new List<ChatResponseUpdate>();
        await foreach (var update in stream)
        {
            results.Add(update);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Fallback", results[0].Text);
        Assert.Equal(" response", results[1].Text);
    }

    [Fact]
    public async Task GetResponseAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Setup mock to throw OperationCanceledException when token is cancelled
        _smartRouterMock.Setup(x => x.GetCandidatesAsync("gpt-4", false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _client.GetResponseAsync(messages, options, cts.Token));
    }

    [Fact]
    public async Task GetResponseAsync_RetryPolicy_RetriesOnTransientError()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };
        var candidate = new EnrichedCandidate(
            new ProviderConfig { Key = "openai", Type = "openai" },
            null,
            "gpt-4");

        var mockChatClient = CreateMockChatClient();
        var mockStrategy = new Mock<IChatClientStrategy>();
        var callCount = 0;

        _smartRouterMock.Setup(x => x.GetCandidatesAsync("gpt-4", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrichedCandidate> { candidate });
        _quotaTrackerMock.Setup(x => x.IsHealthyAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _chatClientFactoryMock.Setup(x => x.GetClient("openai"))
            .Returns(mockChatClient.Object);
        _strategiesMock.Setup(x => x.GetEnumerator())
            .Returns(new List<IChatClientStrategy> { mockStrategy.Object }.GetEnumerator());
        mockStrategy.Setup(x => x.CanHandle("openai"))
            .Returns(true);
        mockStrategy.Setup(x => x.ExecuteAsync(mockChatClient.Object, It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new Exception("Transient error");
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Success after retry")));
            });

        // Act
        var result = await _client.GetResponseAsync(messages, options);

        // Assert
        Assert.Equal("Success after retry", result.Messages.First().Text);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void Constructor_WithNullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IChatClientFactory factory = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SmartRoutingChatClient(
            factory,
            _smartRouterMock.Object,
            _healthStoreMock.Object,
            _quotaTrackerMock.Object,
            _pipelineProviderMock.Object,
            _strategiesMock.Object,
            _activitySource,
            _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullRouter_ThrowsArgumentNullException()
    {
        // Arrange
        ISmartRouter router = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SmartRoutingChatClient(
            _chatClientFactoryMock.Object,
            router,
            _healthStoreMock.Object,
            _quotaTrackerMock.Object,
            _pipelineProviderMock.Object,
            _strategiesMock.Object,
            _activitySource,
            _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullHealthStore_ThrowsArgumentNullException()
    {
        // Arrange
        IHealthStore healthStore = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SmartRoutingChatClient(
            _chatClientFactoryMock.Object,
            _smartRouterMock.Object,
            healthStore,
            _quotaTrackerMock.Object,
            _pipelineProviderMock.Object,
            _strategiesMock.Object,
            _activitySource,
            _loggerMock.Object));
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> GenerateStreamingResponse(string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
            await Task.Yield();
        }
    }


#pragma warning disable CS0162
    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowingStream(Exception ex)
    {
        throw ex;
        yield break;
    }
#pragma warning restore CS0162
}

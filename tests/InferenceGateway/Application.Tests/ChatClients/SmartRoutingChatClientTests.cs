using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.Registry;
using Synaxis.InferenceGateway.Application.ChatClients;
using Synaxis.InferenceGateway.Application.ChatClients.Strategies;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.Routing;
using System.Diagnostics;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

public class SmartRoutingChatClientTests
{
    private readonly Mock<IChatClientFactory> _chatClientFactoryMock;
    private readonly Mock<ISmartRouter> _smartRouterMock;
    private readonly Mock<IHealthStore> _healthStoreMock;
    private readonly Mock<IQuotaTracker> _quotaTrackerMock;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly Mock<IChatClientStrategy> _strategyMock;
    private readonly ActivitySource _activitySource;
    private readonly Mock<ILogger<SmartRoutingChatClient>> _loggerMock;
    private readonly SmartRoutingChatClient _client;

    public SmartRoutingChatClientTests()
    {
        _chatClientFactoryMock = new Mock<IChatClientFactory>();
        _smartRouterMock = new Mock<ISmartRouter>();
        _healthStoreMock = new Mock<IHealthStore>();
        _quotaTrackerMock = new Mock<IQuotaTracker>();
        _loggerMock = new Mock<ILogger<SmartRoutingChatClient>>();
        _strategyMock = new Mock<IChatClientStrategy>();

        var pipelineRegistry = new ResiliencePipelineRegistry<string>();
        pipelineRegistry.TryAddBuilder("provider-retry", (builder, context) => { });
        _pipelineProvider = pipelineRegistry;

        _activitySource = new ActivitySource("Test");

        _client = new SmartRoutingChatClient(
            _chatClientFactoryMock.Object,
            _smartRouterMock.Object,
            _healthStoreMock.Object,
            _quotaTrackerMock.Object,
            _pipelineProvider,
            new[] { _strategyMock.Object },
            _activitySource,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetResponseAsync_ResolvesAndExecutesOneProvider()
    {
        // Arrange
        var modelId = "test-model";
        var providerKey = "provider-1";
        var messages = new[] { new ChatMessage(ChatRole.User, "hi") };
        var chatOptions = new ChatOptions { ModelId = modelId };

        var config = new ProviderConfig { Key = providerKey };
        var candidate = new EnrichedCandidate(config, null, "canonical-model");

        _smartRouterMock.Setup(x => x.GetCandidatesAsync(modelId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrichedCandidate> { candidate });

        _quotaTrackerMock.Setup(x => x.IsHealthyAsync(providerKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var innerClientMock = new Mock<IChatClient>();
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"));
        
        _strategyMock.Setup(x => x.CanHandle(It.IsAny<string>())).Returns(true);
        _strategyMock.Setup(x => x.ExecuteAsync(innerClientMock.Object, It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _chatClientFactoryMock.Setup(x => x.GetClient(providerKey)).Returns(innerClientMock.Object);

        // Act
        var response = await _client.GetResponseAsync(messages, chatOptions);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("response", response.Messages[0].Text);
        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal(providerKey, response.AdditionalProperties["provider_name"]);
        Assert.Equal(providerKey, response.AdditionalProperties["provider_name"]);
        Assert.Equal("canonical-model", response.AdditionalProperties["model_id"]);

        _healthStoreMock.Verify(x => x.MarkSuccessAsync(providerKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.Registry;
using Synaxis.InferenceGateway.Application.ChatClients;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.Routing;
using System.Diagnostics;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

public class SmartRoutingChatClientTests
{
    private readonly Mock<IKeyedServiceProvider> _serviceProviderMock;
    private readonly Mock<IModelResolver> _modelResolverMock;
    private readonly Mock<ICostService> _costServiceMock;
    private readonly Mock<IHealthStore> _healthStoreMock;
    private readonly Mock<IQuotaTracker> _quotaTrackerMock;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly ActivitySource _activitySource;
    private readonly Mock<ILogger<SmartRoutingChatClient>> _loggerMock;
    private readonly SmartRoutingChatClient _client;

    public SmartRoutingChatClientTests()
    {
        _serviceProviderMock = new Mock<IKeyedServiceProvider>();
        _modelResolverMock = new Mock<IModelResolver>();
        _costServiceMock = new Mock<ICostService>();
        _healthStoreMock = new Mock<IHealthStore>();
        _quotaTrackerMock = new Mock<IQuotaTracker>();
        _loggerMock = new Mock<ILogger<SmartRoutingChatClient>>();

        // Mock Polly pipeline to just execute callback
        var pipelineRegistry = new ResiliencePipelineRegistry<string>();
        pipelineRegistry.TryAddBuilder("provider-retry", (builder, context) =>
        {
            // Simple pass-through pipeline
        });
        _pipelineProvider = pipelineRegistry;

        _activitySource = new ActivitySource("Test");

        _client = new SmartRoutingChatClient(
            _serviceProviderMock.Object,
            _modelResolverMock.Object,
            _costServiceMock.Object,
            _healthStoreMock.Object,
            _quotaTrackerMock.Object,
            _pipelineProvider,
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
        var resolution = new ResolutionResult(modelId, new CanonicalModelId(providerKey, "canonical-model"), new List<ProviderConfig> { config });

        _modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.Is<RequiredCapabilities>(c => !c.Streaming), null))
            .ReturnsAsync(resolution);

        _healthStoreMock.Setup(x => x.IsHealthyAsync(providerKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync(providerKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var innerClientMock = new Mock<IChatClient>();
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"));
        innerClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _serviceProviderMock.Setup(x => x.GetKeyedService(typeof(IChatClient), providerKey)).Returns(innerClientMock.Object);

        // Act
        var response = await _client.GetResponseAsync(messages, chatOptions);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("response", response.Messages[0].Text);
        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal(providerKey, response.AdditionalProperties["provider_name"]);
        Assert.Equal("canonical-model", response.AdditionalProperties["model_id"]);

        _healthStoreMock.Verify(x => x.MarkSuccessAsync(providerKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}

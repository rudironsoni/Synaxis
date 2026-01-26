using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Application.ChatClients;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;
using Polly;
using Polly.Registry;
using System.Diagnostics;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

public class SmartRoutingChatClientTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IKeyedServiceProvider> _keyedServiceProviderMock;
    private readonly Mock<IModelResolver> _modelResolverMock;
    private readonly Mock<ICostService> _costServiceMock;
    private readonly Mock<IHealthStore> _healthStoreMock;
    private readonly Mock<IQuotaTracker> _quotaTrackerMock;
    private readonly Mock<ResiliencePipelineProvider<string>> _pipelineProviderMock;
    private readonly Mock<ILogger<SmartRoutingChatClient>> _loggerMock;
    private readonly SmartRoutingChatClient _sut;

    public SmartRoutingChatClientTests()
    {
        _keyedServiceProviderMock = new Mock<IKeyedServiceProvider>();
        _serviceProviderMock = _keyedServiceProviderMock.As<IServiceProvider>();
        
        _modelResolverMock = new Mock<IModelResolver>();
        _costServiceMock = new Mock<ICostService>();
        _healthStoreMock = new Mock<IHealthStore>();
        _quotaTrackerMock = new Mock<IQuotaTracker>();
        _pipelineProviderMock = new Mock<ResiliencePipelineProvider<string>>();
        _loggerMock = new Mock<ILogger<SmartRoutingChatClient>>();

        _pipelineProviderMock.Setup(x => x.GetPipeline(It.IsAny<string>())).Returns(ResiliencePipeline.Empty);

        _sut = new SmartRoutingChatClient(
            _serviceProviderMock.Object,
            _modelResolverMock.Object,
            _costServiceMock.Object,
            _healthStoreMock.Object,
            _quotaTrackerMock.Object,
            _pipelineProviderMock.Object,
            new ActivitySource("test"),
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetResponseAsync_ShouldSkipUnhealthyProviders()
    {
        // Arrange
        var modelId = "test-model";
        var provider1 = new ProviderConfig { Key = "p1" };
        var provider2 = new ProviderConfig { Key = "p2" };
        
        SetupModelResolution(modelId, provider1, provider2);
        
        _healthStoreMock.Setup(x => x.IsHealthyAsync("p1", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _healthStoreMock.Setup(x => x.IsHealthyAsync("p2", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var mockClient2 = new Mock<IChatClient>();
        mockClient2.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "hello")));

        _keyedServiceProviderMock
            .Setup(x => x.GetKeyedService(typeof(IChatClient), "p2"))
            .Returns(mockClient2.Object);

        // Act
        await _sut.GetResponseAsync(new List<ChatMessage>(), new ChatOptions { ModelId = modelId });

        // Assert
        _healthStoreMock.Verify(x => x.IsHealthyAsync("p1", It.IsAny<CancellationToken>()), Times.Once);
        _keyedServiceProviderMock.Verify(x => x.GetKeyedService(typeof(IChatClient), "p1"), Times.Never);
        _keyedServiceProviderMock.Verify(x => x.GetKeyedService(typeof(IChatClient), "p2"), Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_ShouldSkipProvidersOverQuota()
    {
        // Arrange
        var modelId = "test-model";
        var provider1 = new ProviderConfig { Key = "p1" };
        
        SetupModelResolution(modelId, provider1);
        
        _healthStoreMock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync("p1", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => 
            _sut.GetResponseAsync(new List<ChatMessage>(), new ChatOptions { ModelId = modelId }));
        
        _keyedServiceProviderMock.Verify(x => x.GetKeyedService(typeof(IChatClient), "p1"), Times.Never);
    }

    [Fact]
    public async Task GetResponseAsync_ShouldSortByCostAndFreeTier()
    {
        // Arrange
        var modelId = "test-model";
        var pFree = new ProviderConfig { Key = "free" };
        var pCheap = new ProviderConfig { Key = "cheap" };
        var pExpensive = new ProviderConfig { Key = "expensive" };
        
        SetupModelResolution(modelId, pExpensive, pFree, pCheap);
        
        _healthStoreMock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _costServiceMock.Setup(x => x.GetCostAsync("free", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelCost { FreeTier = true, CostPerToken = 0 });
        _costServiceMock.Setup(x => x.GetCostAsync("cheap", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelCost { FreeTier = false, CostPerToken = 0.1m });
        _costServiceMock.Setup(x => x.GetCostAsync("expensive", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelCost { FreeTier = false, CostPerToken = 1.0m });

        var callOrder = new List<string>();
        SetupMockClient("free", callOrder);
        SetupMockClient("cheap", callOrder);
        SetupMockClient("expensive", callOrder);

        // Act
        // We force failure on all to see the order of attempts in callOrder
        await Assert.ThrowsAsync<AggregateException>(() => 
            _sut.GetResponseAsync(new List<ChatMessage>(), new ChatOptions { ModelId = modelId }));

        // Assert
        Assert.Equal(new[] { "free", "cheap", "expensive" }, callOrder);
    }

    [Fact]
    public async Task GetResponseAsync_ShouldCallSelectedProvider()
    {
        // Arrange
        var modelId = "test-model";
        var provider1 = new ProviderConfig { Key = "p1" };
        SetupModelResolution(modelId, provider1);
        
        _healthStoreMock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var mockClient = new Mock<IChatClient>();
        mockClient.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "success")));

        _keyedServiceProviderMock
            .Setup(x => x.GetKeyedService(typeof(IChatClient), "p1"))
            .Returns(mockClient.Object);

        // Act
        var response = await _sut.GetResponseAsync(new List<ChatMessage>(), new ChatOptions { ModelId = modelId });

        // Assert
        Assert.Equal("success", response.Messages[0].Text);
        _healthStoreMock.Verify(x => x.MarkSuccessAsync("p1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_ShouldHandleFailureAndTryNext()
    {
        // Arrange
        var modelId = "test-model";
        var p1 = new ProviderConfig { Key = "p1" };
        var p2 = new ProviderConfig { Key = "p2" };
        SetupModelResolution(modelId, p1, p2);
        
        _healthStoreMock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Ensure p1 is tried first by making it free/cheaper
        _costServiceMock.Setup(x => x.GetCostAsync("p1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelCost { FreeTier = true });
        _costServiceMock.Setup(x => x.GetCostAsync("p2", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelCost { FreeTier = false });

        var mockClient1 = new Mock<IChatClient>();
        mockClient1.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("p1 failed"));

        var mockClient2 = new Mock<IChatClient>();
        mockClient2.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "p2 success")));

        _keyedServiceProviderMock.Setup(x => x.GetKeyedService(typeof(IChatClient), "p1")).Returns(mockClient1.Object);
        _keyedServiceProviderMock.Setup(x => x.GetKeyedService(typeof(IChatClient), "p2")).Returns(mockClient2.Object);

        // Act
        var response = await _sut.GetResponseAsync(new List<ChatMessage>(), new ChatOptions { ModelId = modelId });

        // Assert
        Assert.Equal("p2 success", response.Messages[0].Text);
        _healthStoreMock.Verify(x => x.MarkFailureAsync("p1", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _healthStoreMock.Verify(x => x.MarkSuccessAsync("p2", It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupModelResolution(string modelId, params ProviderConfig[] candidates)
    {
        _modelResolverMock.Setup(x => x.ResolveAsync(modelId, It.IsAny<EndpointKind>(), It.IsAny<RequiredCapabilities>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new ResolutionResult(modelId, new CanonicalModelId("test", "test"), candidates.ToList()));
    }

    private void SetupMockClient(string key, List<string> callOrder)
    {
        var mockClient = new Mock<IChatClient>();
        mockClient.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add(key))
            .ThrowsAsync(new Exception("fail"));

        _keyedServiceProviderMock
            .Setup(x => x.GetKeyedService(typeof(IChatClient), key))
            .Returns(mockClient.Object);
    }
}

using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Routing;

public class SmartRouterTests
{
    private readonly Mock<IModelResolver> _modelResolverMock;
    private readonly Mock<ICostService> _costServiceMock;
    private readonly Mock<IHealthStore> _healthStoreMock;
    private readonly Mock<IQuotaTracker> _quotaTrackerMock;
    private readonly Mock<ILogger<SmartRouter>> _loggerMock;
    private readonly SmartRouter _router;

    public SmartRouterTests()
    {
        _modelResolverMock = new Mock<IModelResolver>();
        _costServiceMock = new Mock<ICostService>();
        _healthStoreMock = new Mock<IHealthStore>();
        _quotaTrackerMock = new Mock<IQuotaTracker>();
        _loggerMock = new Mock<ILogger<SmartRouter>>();

        _router = new SmartRouter(
            _modelResolverMock.Object,
            _costServiceMock.Object,
            _healthStoreMock.Object,
            _quotaTrackerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetCandidatesAsync_ReturnsFilteredAndSortedCandidates()
    {
        // Arrange
        var modelId = "test-model";
        var unhealthyKey = "unhealthy";
        var expensiveKey = "expensive";
        var cheapKey = "cheap";

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("canonical", "canonical"),
            new List<ProviderConfig>
            {
                new ProviderConfig { Key = unhealthyKey },
                new ProviderConfig { Key = expensiveKey, Tier = 2 },
                new ProviderConfig { Key = cheapKey, Tier = 1 }
            });

        _modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        // Health: Unhealthy one fails
        _healthStoreMock.Setup(x => x.IsHealthyAsync(unhealthyKey, default)).ReturnsAsync(false);
        _healthStoreMock.Setup(x => x.IsHealthyAsync(expensiveKey, default)).ReturnsAsync(true);
        _healthStoreMock.Setup(x => x.IsHealthyAsync(cheapKey, default)).ReturnsAsync(true);

        // Quota: All pass
        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), default)).ReturnsAsync(true);

        // Cost
        _costServiceMock.Setup(x => x.GetCostAsync(expensiveKey, "default", default))
            .ReturnsAsync(new ModelCost { CostPerToken = 0.02m });
        _costServiceMock.Setup(x => x.GetCostAsync(cheapKey, "default", default))
            .ReturnsAsync(new ModelCost { CostPerToken = 0.01m });

        // Act
        var result = await _router.GetCandidatesAsync(modelId, false);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(cheapKey, result[0].Key); // Cheap one first
        Assert.Equal(expensiveKey, result[1].Key);
    }
}

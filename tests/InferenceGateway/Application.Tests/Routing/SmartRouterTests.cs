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

    [Fact]
    public async Task GetCandidatesAsync_NoProvidersFound_ThrowsArgumentException()
    {
        // Arrange
        var modelId = "unknown-model";
        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("canonical", "canonical"),
            new List<ProviderConfig>());

        _modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _router.GetCandidatesAsync(modelId, false));
        Assert.Contains($"No providers available for model '{modelId}'", ex.Message);
    }

    [Fact]
    public async Task GetCandidatesAsync_AllProvidersUnhealthy_ReturnsEmptyList()
    {
        // Arrange
        var modelId = "test-model";
        var provider1 = "provider1";
        var provider2 = "provider2";

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("canonical", "canonical"),
            new List<ProviderConfig>
            {
                new ProviderConfig { Key = provider1 },
                new ProviderConfig { Key = provider2 }
            });

        _modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        _healthStoreMock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);

        _costServiceMock.Setup(x => x.GetCostAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync((ModelCost?)null);

        // Act
        var result = await _router.GetCandidatesAsync(modelId, false);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_AllProvidersQuotaExceeded_ReturnsEmptyList()
    {
        // Arrange
        var modelId = "test-model";
        var provider1 = "provider1";

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("canonical", "canonical"),
            new List<ProviderConfig>
            {
                new ProviderConfig { Key = provider1 }
            });

        _modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        _healthStoreMock.Setup(x => x.IsHealthyAsync(provider1, default))
            .ReturnsAsync(true);

        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync(provider1, default))
            .ReturnsAsync(false);

        // Act
        var result = await _router.GetCandidatesAsync(modelId, false);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidatesAsync_FreeProviderSortedFirst()
    {
        // Arrange
        var modelId = "test-model";
        var freeProvider = "free-provider";
        var paidProvider = "paid-provider";

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("canonical", "canonical"),
            new List<ProviderConfig>
            {
                new ProviderConfig { Key = freeProvider, Tier = 2 },
                new ProviderConfig { Key = paidProvider, Tier = 1 }
            });

        _modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), It.IsAny<Guid?>()))
            .ReturnsAsync(resolution);

        _healthStoreMock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _costServiceMock.Setup(x => x.GetCostAsync(freeProvider, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelCost { CostPerToken = 0m, FreeTier = true });
        _costServiceMock.Setup(x => x.GetCostAsync(paidProvider, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelCost { CostPerToken = 0.01m, FreeTier = false });

        // Act
        var result = await _router.GetCandidatesAsync(modelId, false);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(freeProvider, result[0].Key); // Free provider first
        Assert.True(result[0].IsFree);
        Assert.False(result[1].IsFree);
    }

    [Fact]
    public async Task GetCandidatesAsync_StreamingCapabilityPassedToResolver()
    {
        // Arrange
        var modelId = "test-model";
        RequiredCapabilities? capturedCaps = null;

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("canonical", "canonical"),
            new List<ProviderConfig>());

        _modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), It.IsAny<Guid?>()))
            .Callback<string, EndpointKind, RequiredCapabilities?, Guid?>((_, _, caps, _) => capturedCaps = caps)
            .ReturnsAsync(resolution);

        // Act
        try
        {
            await _router.GetCandidatesAsync(modelId, true);
        }
        catch (ArgumentException)
        {
            // Expected - no providers
        }

        // Assert
        Assert.NotNull(capturedCaps);
        Assert.True(capturedCaps!.Streaming);
    }

    [Fact]
    public async Task GetCandidatesAsync_TierBreaksTiesWhenCostEqual()
    {
        // Arrange
        var modelId = "test-model";
        var highTier = "high-tier";
        var lowTier = "low-tier";

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("canonical", "canonical"),
            new List<ProviderConfig>
            {
                new ProviderConfig { Key = highTier, Tier = 2 },
                new ProviderConfig { Key = lowTier, Tier = 1 }
            });

        _modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        _healthStoreMock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);

        _quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);

        // Same cost
        _costServiceMock.Setup(x => x.GetCostAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new ModelCost { CostPerToken = 0.01m });

        // Act
        var result = await _router.GetCandidatesAsync(modelId, false);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(lowTier, result[0].Key); // Lower tier first
        Assert.Equal(highTier, result[1].Key);
    }
}

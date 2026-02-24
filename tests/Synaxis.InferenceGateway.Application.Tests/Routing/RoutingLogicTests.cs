using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Routing;

/// <summary>
/// Comprehensive unit tests for routing logic including:
/// - Provider routing by model ID
/// - Tier failover logic
/// - Canonical model resolution
/// - Alias resolution
/// </summary>
public class RoutingLogicTests
{
    private readonly Mock<IOptions<SynaxisConfiguration>> _configMock;
    private readonly Mock<IProviderRegistry> _registryMock;
    private readonly Mock<IControlPlaneStore> _storeMock;
    private readonly Mock<IModelResolver> _modelResolverMock;
    private readonly Mock<ICostService> _costServiceMock;
    private readonly Mock<IHealthStore> _healthStoreMock;
    private readonly Mock<IQuotaTracker> _quotaTrackerMock;
    private readonly Mock<IRoutingScoreCalculator> _routingScoreCalculatorMock;
    private readonly Mock<ILogger<SmartRouter>> _loggerMock;
    private readonly ModelResolver _resolver;
    private readonly SmartRouter _router;

    public RoutingLogicTests()
    {
        this._configMock = new Mock<IOptions<SynaxisConfiguration>>();
        this._registryMock = new Mock<IProviderRegistry>();
        this._storeMock = new Mock<IControlPlaneStore>();
        this._modelResolverMock = new Mock<IModelResolver>();
        this._costServiceMock = new Mock<ICostService>();
        this._healthStoreMock = new Mock<IHealthStore>();
        this._quotaTrackerMock = new Mock<IQuotaTracker>();
        this._routingScoreCalculatorMock = new Mock<IRoutingScoreCalculator>();
        this._loggerMock = new Mock<ILogger<SmartRouter>>();

        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["groq"] = new ProviderConfig { Type = "groq", Tier = 0, Models = ["llama-3.1-70b-versatile"], Enabled = true },
                ["openai"] = new ProviderConfig { Type = "openai", Tier = 1, Models = ["gpt-4"], Enabled = true },
                ["deepseek"] = new ProviderConfig { Type = "openai", Tier = 2, Models = ["deepseek-chat"], Enabled = true },
                ["cohere"] = new ProviderConfig { Type = "cohere", Tier = 0, Models = ["command-r-plus"], Enabled = true },
            },
            CanonicalModels = new List<CanonicalModelConfig>
            {
                new CanonicalModelConfig { Id = "llama-3.1-70b-versatile", Provider = "groq", ModelPath = "llama-3.1-70b-versatile", Streaming = true, Tools = true },
                new CanonicalModelConfig { Id = "gpt-4", Provider = "openai", ModelPath = "gpt-4", Streaming = true, Tools = true, Vision = true },
                new CanonicalModelConfig { Id = "deepseek-chat", Provider = "deepseek", ModelPath = "deepseek-chat", Streaming = true, Tools = false },
                new CanonicalModelConfig { Id = "command-r-plus", Provider = "cohere", ModelPath = "command-r-plus", Streaming = true, Tools = true },
            },
            Aliases = new Dictionary<string, AliasConfig>
            {
                ["default"] = new AliasConfig { Candidates = ["gpt-4", "llama-3.1-70b-versatile", "deepseek-chat"] },
                ["fast"] = new AliasConfig { Candidates = ["llama-3.1-70b-versatile", "command-r-plus"] },
                ["smart"] = new AliasConfig { Candidates = ["gpt-4", "deepseek-chat"] }
            },
        };

        this._configMock.Setup(x => x.Value).Returns(config);

        this._resolver = new ModelResolver(this._configMock.Object, this._registryMock.Object, this._storeMock.Object);
        this._router = new SmartRouter(
            this._modelResolverMock.Object,
            this._costServiceMock.Object,
            this._healthStoreMock.Object,
            this._quotaTrackerMock.Object,
            this._routingScoreCalculatorMock.Object,
            this._loggerMock.Object);
    }

    #region Provider Routing Tests

    [Fact]
    public void ProviderRouting_WithValidModelId_ReturnsCorrectProvider()
    {
        // Arrange
        var modelId = "gpt-4";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
        Assert.Equal("openai", result.Candidates[0].Key);
    }

    [Fact]
    public void ProviderRouting_WithMultipleProviders_ReturnsAllMatchingProviders()
    {
        // Arrange
        var modelId = "llama-3.1-70b-versatile";
        this._registryMock.Setup(x => x.GetCandidates("llama-3.1-70b-versatile"))
            .Returns([("groq", 0), ("cohere", 0)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("groq", result.CanonicalId.Provider);
        Assert.Equal("llama-3.1-70b-versatile", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
        Assert.Equal("groq", result.Candidates[0].Key);
    }

    [Fact]
    public void ProviderRouting_WithUnknownModelId_ReturnsEmptyCandidates()
    {
        // Arrange
        var modelId = "unknown-model";
        this._registryMock.Setup(x => x.GetCandidates("unknown-model"))
            .Returns([]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("unknown", result.CanonicalId.Provider);
        Assert.Equal(modelId, result.CanonicalId.ModelPath);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void ProviderRouting_WithWildcardModel_ReturnsAllProviders()
    {
        // Arrange
        var modelId = "*";
        this._registryMock.Setup(x => x.GetCandidates("*"))
            .Returns([("groq", 0), ("openai", 1), ("deepseek", 2)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal(3, result.Candidates.Count);
    }

    [Fact]
    public void ProviderRouting_WithDisabledProvider_ExcludesProvider()
    {
        // Arrange
        var modelId = "gpt-4";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1), ("disabled-provider", 2)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Single(result.Candidates);
        Assert.Equal("openai", result.Candidates[0].Key);
    }

    #endregion

    #region Tier Failover Tests

    [Fact]
    public async Task TierFailover_WithTier0Healthy_UsesTier0()
    {
        // Arrange
        var modelId = "test-model";
        var tier0Provider = "groq";
        var tier1Provider = "openai";

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("groq", "llama-3.1-70b-versatile"),
            new List<ProviderConfig>
            {
                new ProviderConfig { Key = tier0Provider, Tier = 0 },
                new ProviderConfig { Key = tier1Provider, Tier = 1 },
            });

        this._modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        this._healthStoreMock.Setup(x => x.IsHealthyAsync(tier0Provider, default)).ReturnsAsync(true);
        this._healthStoreMock.Setup(x => x.IsHealthyAsync(tier1Provider, default)).ReturnsAsync(true);
        this._quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), default)).ReturnsAsync(true);
        this._costServiceMock.Setup(x => x.GetCostAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new ModelCost { CostPerToken = 0.01m });

        // Act
        var result = await this._router.GetCandidatesAsync(modelId, false);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(tier0Provider, result[0].Key); // Tier 0 first
        Assert.Equal(tier1Provider, result[1].Key);
    }

    [Fact]
    public async Task TierFailover_WithTier0Unhealthy_FailsOverToTier1()
    {
        // Arrange
        var modelId = "test-model";
        var tier0Provider = "groq";
        var tier1Provider = "openai";

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("groq", "llama-3.1-70b-versatile"),
            new List<ProviderConfig>
            {
                new ProviderConfig { Key = tier0Provider, Tier = 0 },
                new ProviderConfig { Key = tier1Provider, Tier = 1 },
            });

        this._modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        this._healthStoreMock.Setup(x => x.IsHealthyAsync(tier0Provider, default)).ReturnsAsync(false);
        this._healthStoreMock.Setup(x => x.IsHealthyAsync(tier1Provider, default)).ReturnsAsync(true);
        this._quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), default)).ReturnsAsync(true);
        this._costServiceMock.Setup(x => x.GetCostAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new ModelCost { CostPerToken = 0.01m });

        // Act
        var result = await this._router.GetCandidatesAsync(modelId, false);

        // Assert
        Assert.Single(result);
        Assert.Equal(tier1Provider, result[0].Key); // Only tier 1 available
    }

    [Fact]
    public async Task TierFailover_WithTier0QuotaExceeded_FailsOverToTier1()
    {
        // Arrange
        var modelId = "test-model";
        var tier0Provider = "groq";
        var tier1Provider = "openai";

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("groq", "llama-3.1-70b-versatile"),
            new List<ProviderConfig>
            {
                new ProviderConfig { Key = tier0Provider, Tier = 0 },
                new ProviderConfig { Key = tier1Provider, Tier = 1 },
            });

        this._modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        this._healthStoreMock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), default)).ReturnsAsync(true);
        this._quotaTrackerMock.Setup(x => x.CheckQuotaAsync(tier0Provider, default)).ReturnsAsync(false);
        this._quotaTrackerMock.Setup(x => x.CheckQuotaAsync(tier1Provider, default)).ReturnsAsync(true);
        this._costServiceMock.Setup(x => x.GetCostAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new ModelCost { CostPerToken = 0.01m });

        // Act
        var result = await this._router.GetCandidatesAsync(modelId, false);

        // Assert
        Assert.Single(result);
        Assert.Equal(tier1Provider, result[0].Key); // Only tier 1 available
    }

    [Fact]
    public async Task TierFailover_WithAllTiersUnavailable_ReturnsEmptyList()
    {
        // Arrange
        var modelId = "test-model";
        var tier0Provider = "groq";
        var tier1Provider = "openai";

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("groq", "llama-3.1-70b-versatile"),
            new List<ProviderConfig>
            {
                new ProviderConfig { Key = tier0Provider, Tier = 0 },
                new ProviderConfig { Key = tier1Provider, Tier = 1 },
            });

        this._modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        this._healthStoreMock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        this._quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), default)).ReturnsAsync(true);

        // Act
        var result = await this._router.GetCandidatesAsync(modelId, false);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task TierFailover_WithMultipleProvidersInSameTier_SortsByCost()
    {
        // Arrange
        var modelId = "test-model";
        var expensiveProvider = "openai";
        var cheapProvider = "deepseek";

        var resolution = new ResolutionResult(
            modelId,
            new CanonicalModelId("openai", "gpt-4"),
            new List<ProviderConfig>
            {
                new ProviderConfig { Key = expensiveProvider, Tier = 1 },
                new ProviderConfig { Key = cheapProvider, Tier = 1 },
            });

        this._modelResolverMock.Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        this._healthStoreMock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), default)).ReturnsAsync(true);
        this._quotaTrackerMock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), default)).ReturnsAsync(true);
        this._costServiceMock.Setup(x => x.GetCostAsync(expensiveProvider, It.IsAny<string>(), default))
            .ReturnsAsync(new ModelCost { CostPerToken = 0.02m });
        this._costServiceMock.Setup(x => x.GetCostAsync(cheapProvider, It.IsAny<string>(), default))
            .ReturnsAsync(new ModelCost { CostPerToken = 0.01m });

        // Setup score calculator to score cheaper provider higher
        this._routingScoreCalculatorMock.Setup(x => x.CalculateScoreAsync(
            It.Is<EnrichedCandidate>(c => c.Key == cheapProvider),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(84.0); // Cheaper = higher score
        this._routingScoreCalculatorMock.Setup(x => x.CalculateScoreAsync(
            It.Is<EnrichedCandidate>(c => c.Key == expensiveProvider),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(83.0); // More expensive = lower score

        // Act
        var result = await this._router.GetCandidatesAsync(modelId, false);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(cheapProvider, result[0].Key); // Cheaper first
        Assert.Equal(expensiveProvider, result[1].Key);
    }

    #endregion

    #region Canonical Model Resolution Tests

    [Fact]
    public void CanonicalResolution_WithValidCanonicalId_ResolvesCorrectly()
    {
        // Arrange
        var modelId = "gpt-4";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Equal("openai/gpt-4", result.CanonicalId.ToString());
    }

    [Fact]
    public void CanonicalResolution_WithProviderSlashModel_ParsesCorrectly()
    {
        // Arrange
        var modelId = "openai/gpt-4";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
    }

    [Fact]
    public void CanonicalResolution_WithAtPrefix_HandlesCorrectly()
    {
        // Arrange
        var modelId = "@cf/meta/llama-2-7b-chat-int8";
        this._registryMock.Setup(x => x.GetCandidates("@cf/meta/llama-2-7b-chat-int8"))
            .Returns([("cloudflare", 0)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal("unknown", result.CanonicalId.Provider);
        Assert.Equal(modelId, result.CanonicalId.ModelPath);
    }

    [Fact]
    public void CanonicalResolution_WithModelContainingSlash_ParsesCorrectly()
    {
        // Arrange
        var modelId = "meta/llama-2-7b-chat-int8";
        this._registryMock.Setup(x => x.GetCandidates("meta/llama-2-7b-chat-int8"))
            .Returns([]);

        // Fallback to raw string
        this._registryMock.Setup(x => x.GetCandidates(modelId))
            .Returns([("cloudflare", 0)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal("meta", result.CanonicalId.Provider);
        Assert.Equal("llama-2-7b-chat-int8", result.CanonicalId.ModelPath);
    }

    [Fact]
    public void CanonicalResolution_WithCapabilityFilter_RespectsStreaming()
    {
        // Arrange
        var modelId = "gpt-4";
        var required = new RequiredCapabilities { Streaming = true };
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId, required);

        // Assert
        Assert.Single(result.Candidates);
    }

    [Fact]
    public void CanonicalResolution_WithCapabilityFilter_RespectsTools()
    {
        // Arrange
        var modelId = "deepseek-chat";
        var required = new RequiredCapabilities { Tools = true };
        this._registryMock.Setup(x => x.GetCandidates("deepseek-chat"))
            .Returns([("deepseek", 2)]);

        // Act
        var result = this._resolver.Resolve(modelId, required);

        // Assert
        Assert.Empty(result.Candidates); // deepseek-chat doesn't support tools
    }

    [Fact]
    public void CanonicalResolution_WithCapabilityFilter_RespectsVision()
    {
        // Arrange
        var modelId = "gpt-4";
        var required = new RequiredCapabilities { Vision = true };
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId, required);

        // Assert
        Assert.Single(result.Candidates); // gpt-4 supports vision
    }

    [Fact]
    public void CanonicalResolution_WithMultipleCapabilities_RespectsAll()
    {
        // Arrange
        var modelId = "gpt-4";
        var required = new RequiredCapabilities { Streaming = true, Tools = true, Vision = true };
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId, required);

        // Assert
        Assert.Single(result.Candidates);
    }

    [Fact]
    public void CanonicalResolution_WithUnmetCapabilities_SkipsModel()
    {
        // Arrange
        var modelId = "deepseek-chat";
        var required = new RequiredCapabilities { Tools = true, Vision = true };
        this._registryMock.Setup(x => x.GetCandidates("deepseek-chat"))
            .Returns([("deepseek", 2)]);

        // Act
        var result = this._resolver.Resolve(modelId, required);

        // Assert
        Assert.Empty(result.Candidates);
    }

    #endregion

    #region Alias Resolution Tests

    [Fact]
    public void AliasResolution_WithValidAlias_ResolvesToFirstCandidate()
    {
        // Arrange
        var aliasId = "default";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(aliasId);

        // Assert
        Assert.Equal(aliasId, result.OriginalModelId);
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public void AliasResolution_WithMultipleCandidates_TriesInOrder()
    {
        // Arrange
        var aliasId = "default";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([]); // First candidate unavailable
        this._registryMock.Setup(x => x.GetCandidates("llama-3.1-70b-versatile"))
            .Returns([("groq", 0)]); // Second candidate available

        // Act
        var result = this._resolver.Resolve(aliasId);

        // Assert
        Assert.Equal(aliasId, result.OriginalModelId);
        Assert.Equal("groq", result.CanonicalId.Provider);
        Assert.Equal("llama-3.1-70b-versatile", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public void AliasResolution_WithAllCandidatesUnavailable_ReturnsEmpty()
    {
        // Arrange
        var aliasId = "default";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([]);
        this._registryMock.Setup(x => x.GetCandidates("llama-3.1-70b-versatile"))
            .Returns([]);
        this._registryMock.Setup(x => x.GetCandidates("deepseek-chat"))
            .Returns([]);

        // Act
        var result = this._resolver.Resolve(aliasId);

        // Assert
        Assert.Equal(aliasId, result.OriginalModelId);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void AliasResolution_WithNestedAlias_ResolvesToCanonical()
    {
        // Arrange
        var aliasId = "fast";
        this._registryMock.Setup(x => x.GetCandidates("llama-3.1-70b-versatile"))
            .Returns([("groq", 0)]);

        // Act
        var result = this._resolver.Resolve(aliasId);

        // Assert
        Assert.Equal(aliasId, result.OriginalModelId);
        Assert.Equal("groq", result.CanonicalId.Provider);
        Assert.Equal("llama-3.1-70b-versatile", result.CanonicalId.ModelPath);
    }

    [Fact]
    public void AliasResolution_WithUnknownAlias_TreatsAsModelId()
    {
        // Arrange
        var aliasId = "unknown-alias";
        this._registryMock.Setup(x => x.GetCandidates("unknown-alias"))
            .Returns([]);

        // Act
        var result = this._resolver.Resolve(aliasId);

        // Assert
        Assert.Equal(aliasId, result.OriginalModelId);
        Assert.Equal("unknown", result.CanonicalId.Provider);
        Assert.Equal(aliasId, result.CanonicalId.ModelPath);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void AliasResolution_WithEmptyAlias_ReturnsEmpty()
    {
        // Arrange
        var aliasId = "";
        this._registryMock.Setup(x => x.GetCandidates(""))
            .Returns([]);

        // Act
        var result = this._resolver.Resolve(aliasId);

        // Assert
        Assert.Equal(aliasId, result.OriginalModelId);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task AliasResolution_WithTenantAlias_UsesTenantSpecificAlias()
    {
        // Arrange
        var aliasId = "tenant-alias";
        var tenantId = Guid.NewGuid();
        var alias = new ModelAlias { TargetModel = "gpt-4" };

        this._storeMock.Setup(x => x.GetGlobalModelAsync(aliasId))
            .ReturnsAsync((GlobalModel?)null);
        this._storeMock.Setup(x => x.GetAliasAsync(tenantId, aliasId))
            .ReturnsAsync(alias);
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = await this._resolver.ResolveAsync(aliasId, EndpointKind.ChatCompletions, tenantId: tenantId);

        // Assert
        Assert.Equal(aliasId, result.OriginalModelId);
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public async Task AliasResolution_WithModelCombo_UsesComboOrder()
    {
        // Arrange
        var comboId = "combo-model";
        var tenantId = Guid.NewGuid();
        var combo = new ModelCombo { OrderedModelsJson = "[\"gpt-4\", \"llama-3.1-70b-versatile\"]" };

        this._storeMock.Setup(x => x.GetGlobalModelAsync(comboId))
            .ReturnsAsync((GlobalModel?)null);
        this._storeMock.Setup(x => x.GetComboAsync(tenantId, comboId))
            .ReturnsAsync(combo);
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = await this._resolver.ResolveAsync(comboId, EndpointKind.ChatCompletions, tenantId: tenantId);

        // Assert
        Assert.Equal(comboId, result.OriginalModelId);
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public async Task AliasResolution_WithInvalidComboJson_FallsBackToConfig()
    {
        // Arrange
        var comboId = "combo-model";
        var tenantId = Guid.NewGuid();
        var combo = new ModelCombo { OrderedModelsJson = "invalid-json" };

        this._storeMock.Setup(x => x.GetGlobalModelAsync(comboId))
            .ReturnsAsync((GlobalModel?)null);
        this._storeMock.Setup(x => x.GetComboAsync(tenantId, comboId))
            .ReturnsAsync(combo);

        // Act
        var result = await this._resolver.ResolveAsync(comboId, EndpointKind.ChatCompletions, tenantId: tenantId);

        // Assert
        Assert.Equal(comboId, result.OriginalModelId);
        Assert.Equal("unknown", result.CanonicalId.Provider);
        Assert.Equal(comboId, result.CanonicalId.ModelPath);
        Assert.Empty(result.Candidates);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void Resolve_WithNullModelId_ThrowsArgumentNullException()
    {
        // Arrange
        string modelId = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => this._resolver.Resolve(modelId));
    }

    [Fact]
    public void Resolve_WithWhitespaceModelId_ReturnsEmpty()
    {
        // Arrange
        var modelId = "   ";
        this._registryMock.Setup(x => x.GetCandidates("   "))
            .Returns([]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task ResolveAsync_WithNullModelId_ThrowsArgumentNullException()
    {
        // Arrange
        string modelId = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => this._resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions));
    }

    [Fact]
    public async Task GetCandidatesAsync_WithNullModelId_ThrowsArgumentException()
    {
        // Arrange
        string modelId = null!;
        var resolution = new ResolutionResult(
            modelId!,
            new CanonicalModelId("canonical", "canonical"),
            new List<ProviderConfig>());

        this._modelResolverMock.Setup(x => x.ResolveAsync(modelId!, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>(), default))
            .ReturnsAsync(resolution);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => this._router.GetCandidatesAsync(modelId!, false));
    }

    [Fact]
    public void CanonicalModelId_ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var canonicalId = new CanonicalModelId("openai", "gpt-4");

        // Act
        var result = canonicalId.ToString();

        // Assert
        Assert.Equal("openai/gpt-4", result);
    }

    [Fact]
    public void CanonicalModelId_Parse_WithValidInput_ReturnsCorrectId()
    {
        // Arrange
        var input = "openai/gpt-4";

        // Act
        var result = CanonicalModelId.Parse(input);

        // Assert
        Assert.Equal("openai", result.Provider);
        Assert.Equal("gpt-4", result.ModelPath);
    }

    [Fact]
    public void CanonicalModelId_Parse_WithSinglePart_ReturnsUnknownProvider()
    {
        // Arrange
        var input = "gpt-4";

        // Act
        var result = CanonicalModelId.Parse(input);

        // Assert
        Assert.Equal("unknown", result.Provider);
        Assert.Equal("gpt-4", result.ModelPath);
    }

    [Fact]
    public void CanonicalModelId_Parse_WithAtPrefix_ReturnsUnknownProvider()
    {
        // Arrange
        var input = "@cf/meta/llama-2-7b-chat-int8";

        // Act
        var result = CanonicalModelId.Parse(input);

        // Assert
        Assert.Equal("unknown", result.Provider);
        Assert.Equal(input, result.ModelPath);
    }

    #endregion
}

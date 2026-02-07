using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Routing;

public class ModelResolverTests
{
    private readonly Mock<IOptions<SynaxisConfiguration>> _configMock;
    private readonly Mock<IProviderRegistry> _registryMock;
    private readonly Mock<IControlPlaneStore> _storeMock;
    private readonly ModelResolver _resolver;

    public ModelResolverTests()
    {
        this._configMock = new Mock<IOptions<SynaxisConfiguration>>();
        this._registryMock = new Mock<IProviderRegistry>();
        this._storeMock = new Mock<IControlPlaneStore>();

        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["groq"] = new ProviderConfig { Type = "groq", Tier = 0, Models = ["llama-3.1-70b-versatile"], Enabled = true },
                ["openai"] = new ProviderConfig { Type = "openai", Tier = 1, Models = ["gpt-4"], Enabled = true },
                ["deepseek"] = new ProviderConfig { Type = "openai", Tier = 2, Models = ["deepseek-chat"], Enabled = true },
            },
            CanonicalModels = new List<CanonicalModelConfig>
            {
                new CanonicalModelConfig { Id = "llama-3.1-70b-versatile", Provider = "groq", ModelPath = "llama-3.1-70b-versatile", Streaming = true },
                new CanonicalModelConfig { Id = "gpt-4", Provider = "openai", ModelPath = "gpt-4", Streaming = true },
                new CanonicalModelConfig { Id = "deepseek-chat", Provider = "deepseek", ModelPath = "deepseek-chat", Streaming = true },
            },
            Aliases = new Dictionary<string, AliasConfig>
            {
                ["default"] = new AliasConfig { Candidates = ["gpt-4", "llama-3.1-70b-versatile"] },
                ["fast"] = new AliasConfig { Candidates = ["llama-3.1-70b-versatile"] }
            },
        };

        this._configMock.Setup(x => x.Value).Returns(config);

        this._resolver = new ModelResolver(this._configMock.Object, this._registryMock.Object, this._storeMock.Object);
    }

    [Fact]
    public void ResolveModel_WithValidModel_ReturnsProvider()
    {
        // Arrange
        var modelId = "gpt-4";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Equal("openai", result.canonicalId.provider);
        Assert.Equal("gpt-4", result.canonicalId.modelPath);
        Assert.Single(result.candidates);
        Assert.Equal("openai", result.candidates[0].Key);
    }

    [Fact]
    public void ResolveModel_WithInvalidModel_ReturnsNull()
    {
        // Arrange
        var modelId = "invalid-model";
        this._registryMock.Setup(x => x.GetCandidates("invalid-model"))
            .Returns([]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Empty(result.candidates);
    }

    [Fact]
    public void ResolveModel_WithAlias_ResolvesToCanonical()
    {
        // Arrange
        var modelId = "default";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Equal("openai", result.canonicalId.provider);
        Assert.Equal("gpt-4", result.canonicalId.modelPath);
        Assert.Single(result.candidates);
    }

    [Fact]
    public void ResolveModel_WithMultipleCandidates_ReturnsFirstAvailable()
    {
        // Arrange
        var modelId = "default";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);
        this._registryMock.Setup(x => x.GetCandidates("llama-3.1-70b-versatile"))
            .Returns([("groq", 0)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Equal("openai", result.canonicalId.provider);
        Assert.Equal("gpt-4", result.canonicalId.modelPath);
        Assert.Single(result.candidates);
    }

    [Fact]
    public void ResolveModel_AllCandidatesUnavailable_ReturnsNull()
    {
        // Arrange
        var modelId = "default";
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([]);
        this._registryMock.Setup(x => x.GetCandidates("llama-3.1-70b-versatile"))
            .Returns([]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Empty(result.candidates);
    }

    [Fact]
    public void ResolveModel_WithProviderFilter_RespectsFilter()
    {
        // Arrange
        var modelId = "deepseek-chat";
        this._registryMock.Setup(x => x.GetCandidates("deepseek-chat"))
            .Returns([("deepseek", 2), ("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Equal("deepseek", result.canonicalId.provider);
        Assert.Equal("deepseek-chat", result.canonicalId.modelPath);
        Assert.Single(result.candidates);
        Assert.Equal("deepseek", result.candidates[0].Key);
    }

    [Fact]
    public void ResolveModel_WithCapabilityRequirements_RespectsRequirements()
    {
        // Arrange
        var modelId = "gpt-4";
        var required = new RequiredCapabilities { Streaming = true };
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId, required);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Single(result.candidates);
    }

    [Fact]
    public void ResolveModel_WithUnmetCapabilityRequirements_SkipsModel()
    {
        // Arrange
        var modelId = "gpt-4";
        var required = new RequiredCapabilities { Streaming = false, Tools = true };
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = this._resolver.Resolve(modelId, required);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Empty(result.candidates);
    }

    [Fact]
    public void ResolveModel_WithNullModelId_ThrowsArgumentException()
    {
        // Arrange
        string modelId = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => this._resolver.Resolve(modelId));
    }

    [Fact]
    public void ResolveModel_WithEmptyModelId_ReturnsNull()
    {
        // Arrange
        var modelId = "";
        this._registryMock.Setup(x => x.GetCandidates(""))
            .Returns([]);

        // Act
        var result = this._resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Empty(result.candidates);
    }

    [Fact]
    public async Task ResolveAsync_WithGlobalModel_ReturnsFromDatabase()
    {
        // Arrange
        var modelId = "gpt-4";
        var globalModel = new GlobalModel
        {
            Id = "gpt-4",
            ProviderModels = [
                new ProviderModel { ProviderId = "openai", ProviderSpecificId = "gpt-4" }
            ],
        };

        this._storeMock.Setup(x => x.GetGlobalModelAsync(modelId))
            .ReturnsAsync(globalModel);

        // Act
        var result = await this._resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Equal("gpt-4", result.canonicalId.modelPath);
        Assert.Single(result.candidates);
        Assert.Equal("openai", result.candidates[0].Key);
    }

    [Fact]
    public async Task ResolveAsync_WithDisabledProvider_SkipsProvider()
    {
        // Arrange
        var modelId = "gpt-4";
        var globalModel = new GlobalModel
        {
            Id = "gpt-4",
            ProviderModels = [
                new ProviderModel { ProviderId = "disabled-provider", ProviderSpecificId = "gpt-4" }
            ],
        };

        this._storeMock.Setup(x => x.GetGlobalModelAsync(modelId))
            .ReturnsAsync(globalModel);

        // Act
        var result = await this._resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Empty(result.candidates);
    }

    [Fact]
    public async Task ResolveAsync_WithTenantAlias_UsesAlias()
    {
        // Arrange
        var modelId = "alias-model";
        var tenantId = Guid.NewGuid();
        var alias = new ModelAlias { TargetModel = "gpt-4" };

        this._storeMock.Setup(x => x.GetGlobalModelAsync(modelId))
            .ReturnsAsync((GlobalModel?)null);
        this._storeMock.Setup(x => x.GetAliasAsync(tenantId, modelId))
            .ReturnsAsync(alias);
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = await this._resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, tenantId: tenantId);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Equal("openai", result.canonicalId.provider);
        Assert.Equal("gpt-4", result.canonicalId.modelPath);
        Assert.Single(result.candidates);
    }

    [Fact]
    public async Task ResolveAsync_WithModelCombo_UsesCombo()
    {
        // Arrange
        var modelId = "combo-model";
        var tenantId = Guid.NewGuid();
        var combo = new ModelCombo { OrderedModelsJson = "[\"gpt-4\", \"llama-3.1-70b-versatile\"]" };

        this._storeMock.Setup(x => x.GetGlobalModelAsync(modelId))
            .ReturnsAsync((GlobalModel?)null);
        this._storeMock.Setup(x => x.GetComboAsync(tenantId, modelId))
            .ReturnsAsync(combo);
        this._registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = await this._resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, tenantId: tenantId);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Equal("openai", result.canonicalId.provider);
        Assert.Equal("gpt-4", result.canonicalId.modelPath);
        Assert.Single(result.candidates);
    }

    [Fact]
    public async Task ResolveAsync_WithInvalidComboJson_FallsBackToConfig()
    {
        // Arrange
        var modelId = "combo-model";
        var tenantId = Guid.NewGuid();
        var combo = new ModelCombo { OrderedModelsJson = "invalid-json" };

        this._storeMock.Setup(x => x.GetGlobalModelAsync(modelId))
            .ReturnsAsync((GlobalModel?)null);
        this._storeMock.Setup(x => x.GetComboAsync(tenantId, modelId))
            .ReturnsAsync(combo);

        // Act
        var result = await this._resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, tenantId: tenantId);

        // Assert
        Assert.Equal(modelId, result.originalModelId);
        Assert.Equal("unknown", result.canonicalId.provider);
        Assert.Equal(modelId, result.canonicalId.modelPath);
        Assert.Empty(result.candidates);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        IOptions<SynaxisConfiguration> config = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelResolver(config, this._registryMock.Object, this._storeMock.Object));
    }

    [Fact]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        // Arrange
        IProviderRegistry registry = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelResolver(this._configMock.Object, registry, this._storeMock.Object));
    }

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Arrange
        IControlPlaneStore store = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelResolver(this._configMock.Object, this._registryMock.Object, store));
    }
}

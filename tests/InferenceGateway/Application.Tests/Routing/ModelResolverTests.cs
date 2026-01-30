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
        _configMock = new Mock<IOptions<SynaxisConfiguration>>();
        _registryMock = new Mock<IProviderRegistry>();
        _storeMock = new Mock<IControlPlaneStore>();

        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["groq"] = new ProviderConfig { Type = "groq", Tier = 0, Models = ["llama-3.1-70b-versatile"], Enabled = true },
                ["openai"] = new ProviderConfig { Type = "openai", Tier = 1, Models = ["gpt-4"], Enabled = true },
                ["deepseek"] = new ProviderConfig { Type = "openai", Tier = 2, Models = ["deepseek-chat"], Enabled = true }
            },
            CanonicalModels = new List<CanonicalModelConfig>
            {
                new CanonicalModelConfig { Id = "llama-3.1-70b-versatile", Provider = "groq", ModelPath = "llama-3.1-70b-versatile", Streaming = true },
                new CanonicalModelConfig { Id = "gpt-4", Provider = "openai", ModelPath = "gpt-4", Streaming = true },
                new CanonicalModelConfig { Id = "deepseek-chat", Provider = "deepseek", ModelPath = "deepseek-chat", Streaming = true }
            },
            Aliases = new Dictionary<string, AliasConfig>
            {
                ["default"] = new AliasConfig { Candidates = ["gpt-4", "llama-3.1-70b-versatile"] },
                ["fast"] = new AliasConfig { Candidates = ["llama-3.1-70b-versatile"] }
            }
        };

        _configMock.Setup(x => x.Value).Returns(config);

        _resolver = new ModelResolver(_configMock.Object, _registryMock.Object, _storeMock.Object);
    }

    [Fact]
    public void ResolveModel_WithValidModel_ReturnsProvider()
    {
        // Arrange
        var modelId = "gpt-4";
        _registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = _resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
        Assert.Equal("openai", result.Candidates[0].Key);
    }

    [Fact]
    public void ResolveModel_WithInvalidModel_ReturnsNull()
    {
        // Arrange
        var modelId = "invalid-model";
        _registryMock.Setup(x => x.GetCandidates("invalid-model"))
            .Returns([]);

        // Act
        var result = _resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void ResolveModel_WithAlias_ResolvesToCanonical()
    {
        // Arrange
        var modelId = "default";
        _registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = _resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public void ResolveModel_WithMultipleCandidates_ReturnsFirstAvailable()
    {
        // Arrange
        var modelId = "default";
        _registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);
        _registryMock.Setup(x => x.GetCandidates("llama-3.1-70b-versatile"))
            .Returns([("groq", 0)]);

        // Act
        var result = _resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public void ResolveModel_AllCandidatesUnavailable_ReturnsNull()
    {
        // Arrange
        var modelId = "default";
        _registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([]);
        _registryMock.Setup(x => x.GetCandidates("llama-3.1-70b-versatile"))
            .Returns([]);

        // Act
        var result = _resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void ResolveModel_WithProviderFilter_RespectsFilter()
    {
        // Arrange
        var modelId = "deepseek-chat";
        _registryMock.Setup(x => x.GetCandidates("deepseek-chat"))
            .Returns([("deepseek", 2), ("openai", 1)]);

        // Act
        var result = _resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("deepseek", result.CanonicalId.Provider);
        Assert.Equal("deepseek-chat", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
        Assert.Equal("deepseek", result.Candidates[0].Key);
    }

    [Fact]
    public void ResolveModel_WithCapabilityRequirements_RespectsRequirements()
    {
        // Arrange
        var modelId = "gpt-4";
        var required = new RequiredCapabilities { Streaming = true };
        _registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = _resolver.Resolve(modelId, required);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public void ResolveModel_WithUnmetCapabilityRequirements_SkipsModel()
    {
        // Arrange
        var modelId = "gpt-4";
        var required = new RequiredCapabilities { Streaming = false, Tools = true };
        _registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = _resolver.Resolve(modelId, required);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void ResolveModel_WithNullModelId_ThrowsArgumentException()
    {
        // Arrange
        string modelId = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _resolver.Resolve(modelId));
    }

    [Fact]
    public void ResolveModel_WithEmptyModelId_ReturnsNull()
    {
        // Arrange
        var modelId = "";
        _registryMock.Setup(x => x.GetCandidates(""))
            .Returns([]);

        // Act
        var result = _resolver.Resolve(modelId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Empty(result.Candidates);
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
            ]
        };

        _storeMock.Setup(x => x.GetGlobalModelAsync(modelId))
            .ReturnsAsync(globalModel);

        // Act
        var result = await _resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
        Assert.Equal("openai", result.Candidates[0].Key);
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
            ]
        };

        _storeMock.Setup(x => x.GetGlobalModelAsync(modelId))
            .ReturnsAsync(globalModel);

        // Act
        var result = await _resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task ResolveAsync_WithTenantAlias_UsesAlias()
    {
        // Arrange
        var modelId = "alias-model";
        var tenantId = Guid.NewGuid();
        var alias = new ModelAlias { TargetModel = "gpt-4" };

        _storeMock.Setup(x => x.GetGlobalModelAsync(modelId))
            .ReturnsAsync((GlobalModel?)null);
        _storeMock.Setup(x => x.GetAliasAsync(tenantId, modelId))
            .ReturnsAsync(alias);
        _registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = await _resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, tenantId: tenantId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public async Task ResolveAsync_WithModelCombo_UsesCombo()
    {
        // Arrange
        var modelId = "combo-model";
        var tenantId = Guid.NewGuid();
        var combo = new ModelCombo { OrderedModelsJson = "[\"gpt-4\", \"llama-3.1-70b-versatile\"]" };

        _storeMock.Setup(x => x.GetGlobalModelAsync(modelId))
            .ReturnsAsync((GlobalModel?)null);
        _storeMock.Setup(x => x.GetComboAsync(tenantId, modelId))
            .ReturnsAsync(combo);
        _registryMock.Setup(x => x.GetCandidates("gpt-4"))
            .Returns([("openai", 1)]);

        // Act
        var result = await _resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, tenantId: tenantId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("openai", result.CanonicalId.Provider);
        Assert.Equal("gpt-4", result.CanonicalId.ModelPath);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public async Task ResolveAsync_WithInvalidComboJson_FallsBackToConfig()
    {
        // Arrange
        var modelId = "combo-model";
        var tenantId = Guid.NewGuid();
        var combo = new ModelCombo { OrderedModelsJson = "invalid-json" };

        _storeMock.Setup(x => x.GetGlobalModelAsync(modelId))
            .ReturnsAsync((GlobalModel?)null);
        _storeMock.Setup(x => x.GetComboAsync(tenantId, modelId))
            .ReturnsAsync(combo);

        // Act
        var result = await _resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, tenantId: tenantId);

        // Assert
        Assert.Equal(modelId, result.OriginalModelId);
        Assert.Equal("unknown", result.CanonicalId.Provider);
        Assert.Equal(modelId, result.CanonicalId.ModelPath);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        IOptions<SynaxisConfiguration> config = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelResolver(config, _registryMock.Object, _storeMock.Object));
    }

    [Fact]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        // Arrange
        IProviderRegistry registry = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelResolver(_configMock.Object, registry, _storeMock.Object));
    }

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Arrange
        IControlPlaneStore store = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelResolver(_configMock.Object, _registryMock.Object, store));
    }
}
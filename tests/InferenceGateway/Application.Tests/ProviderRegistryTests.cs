using Microsoft.Extensions.Options;
using Moq;
using Synaxis.InferenceGateway.Application.Configuration;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests;

public class ProviderRegistryTests
{
    private readonly Mock<IOptions<SynaxisConfiguration>> _configMock;
    private readonly ProviderRegistry _registry;

    public ProviderRegistryTests()
    {
        this._configMock = new Mock<IOptions<SynaxisConfiguration>>();

        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["groq"] = new ProviderConfig { Type = "groq", Tier = 0, Models = ["llama-3.1-70b-versatile", "*"], Enabled = true },
                ["openai"] = new ProviderConfig { Type = "openai", Tier = 1, Models = ["gpt-4", "gpt-3.5-turbo"], Enabled = true },
                ["deepseek"] = new ProviderConfig { Type = "openai", Tier = 2, Models = ["deepseek-chat"], Enabled = true },
                ["disabled-provider"] = new ProviderConfig { Type = "openai", Tier = 3, Models = ["disabled-model"], Enabled = false }
            },
        };

        this._configMock.Setup(x => x.Value).Returns(config);

        this._registry = new ProviderRegistry(this._configMock.Object);
    }

    [Fact]
    public void GetCandidates_WithValidModel_ReturnsMatchingProviders()
    {
        // Arrange
        var modelId = "gpt-4";

        // Act
        var result = this._registry.GetCandidates(modelId).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("openai", result[0].ServiceKey);
        Assert.Equal(1, result[0].Tier);
    }

    [Fact]
    public void GetCandidates_WithWildcardModel_ReturnsAllProviders()
    {
        // Arrange
        var modelId = "any-model";

        // Act
        var result = this._registry.GetCandidates(modelId).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("groq", result[0].ServiceKey);
        Assert.Equal(0, result[0].Tier);
    }

    [Fact]
    public void GetCandidates_WithMultipleMatchingModels_ReturnsAllMatches()
    {
        // Arrange
        var modelId = "gpt-3.5-turbo";

        // Act
        var result = this._registry.GetCandidates(modelId).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("openai", result[0].ServiceKey);
        Assert.Equal(1, result[0].Tier);
    }

    [Fact]
    public void GetCandidates_WithNoMatchingModels_ReturnsWildcardProvider()
    {
        // Arrange
        var modelId = "non-existent-model";

        // Act
        var result = this._registry.GetCandidates(modelId).ToList();

        // Assert
        // Should return wildcard provider (groq) since no exact matches exist
        Assert.Single(result);
        Assert.Equal("groq", result[0].ServiceKey);
        Assert.Equal(0, result[0].Tier);
    }

    [Fact]
    public void GetCandidates_WithDisabledProvider_ExcludesDisabledProvider()
    {
        // Arrange
        var modelId = "disabled-model";

        // Act
        var result = this._registry.GetCandidates(modelId).ToList();

        // Assert
        // Should return wildcard provider (groq) since disabled provider is excluded
        Assert.Single(result);
        Assert.Equal("groq", result[0].ServiceKey);
        Assert.Equal(0, result[0].Tier);
    }

    [Fact]
    public void GetProvider_WithValidKey_ReturnsProvider()
    {
        // Arrange
        var providerKey = "openai";

        // Act
        var result = this._registry.GetProvider(providerKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("openai", result.Key);
        Assert.Equal("openai", result.Type);
        Assert.Equal(1, result.Tier);
    }

    [Fact]
    public void GetProvider_WithInvalidKey_ReturnsNull()
    {
        // Arrange
        var providerKey = "non-existent-provider";

        // Act
        var result = this._registry.GetProvider(providerKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetProvider_WithDisabledProvider_ReturnsNull()
    {
        // Arrange
        var providerKey = "disabled-provider";

        // Act
        var result = this._registry.GetProvider(providerKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetProvider_SetsKeyProperty()
    {
        // Arrange
        var providerKey = "openai";

        // Act
        var result = this._registry.GetProvider(providerKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(providerKey, result.Key);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        IOptions<SynaxisConfiguration> config = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProviderRegistry(config));
    }

    [Fact]
    public void Constructor_InitializesEmptyRegistry()
    {
        // Arrange
        var emptyConfig = new Mock<IOptions<SynaxisConfiguration>>();
        emptyConfig.Setup(x => x.Value).Returns(new SynaxisConfiguration());

        // Act
        var registry = new ProviderRegistry(emptyConfig.Object);

        // Assert
        Assert.NotNull(registry);
    }

    [Fact]
    public void GetCandidates_WithCaseInsensitiveMatching_ReturnsMatches()
    {
        // Arrange
        var modelId = "GPT-4"; // Different case

        // Act
        var result = this._registry.GetCandidates(modelId).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("openai", result[0].ServiceKey);
    }

    [Fact]
    public void GetCandidates_WithEmptyModelId_ReturnsWildcardMatches()
    {
        // Arrange
        var modelId = "";

        // Act
        var result = this._registry.GetCandidates(modelId).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("groq", result[0].ServiceKey);
    }

    [Fact]
    public void GetCandidates_WithNullModelId_ThrowsArgumentNullException()
    {
        // Arrange
        string modelId = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => this._registry.GetCandidates(modelId));
    }

    [Fact]
    public void GetProvider_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string providerKey = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => this._registry.GetProvider(providerKey));
    }

    [Fact]
    public void GetProvider_WithEmptyKey_ReturnsNull()
    {
        // Arrange
        var providerKey = "";

        // Act
        var result = this._registry.GetProvider(providerKey);

        // Assert
        Assert.Null(result);
    }
}

using Microsoft.Extensions.Configuration;
using Synaxis.InferenceGateway.Application.Configuration;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Configuration;

public class SynaxisConfigurationTests
{
    [Fact]
    public void ConfigurationBinding_LoadsProvidersFromJson()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Groq:Type"] = "Groq",
                ["Synaxis:InferenceGateway:Providers:Groq:Key"] = "test-key",
                ["Synaxis:InferenceGateway:Providers:Groq:Tier"] = "1",
                ["Synaxis:InferenceGateway:Providers:Groq:Models:0"] = "llama-3.1-70b",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Single(config!.Providers);
        Assert.True(config.Providers.ContainsKey("Groq"));
        Assert.Equal("Groq", config.Providers["Groq"].Type);
        Assert.Equal("test-key", config.Providers["Groq"].Key);
        Assert.Equal(1, config.Providers["Groq"].Tier);
        Assert.Single(config.Providers["Groq"].Models);
        Assert.Equal("llama-3.1-70b", config.Providers["Groq"].Models[0]);
    }

    [Fact]
    public void ConfigurationBinding_LoadsCanonicalModelsFromJson()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:CanonicalModels:0:Id"] = "groq/llama-3.3-70b",
                ["Synaxis:InferenceGateway:CanonicalModels:0:Provider"] = "Groq",
                ["Synaxis:InferenceGateway:CanonicalModels:0:ModelPath"] = "llama-3.3-70b-versatile",
                ["Synaxis:InferenceGateway:CanonicalModels:0:Streaming"] = "true",
                ["Synaxis:InferenceGateway:CanonicalModels:0:Tools"] = "true",
                ["Synaxis:InferenceGateway:CanonicalModels:0:Vision"] = "false",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Single(config!.CanonicalModels);
        Assert.Equal("groq/llama-3.3-70b", config.CanonicalModels[0].Id);
        Assert.Equal("Groq", config.CanonicalModels[0].Provider);
        Assert.Equal("llama-3.3-70b-versatile", config.CanonicalModels[0].ModelPath);
        Assert.True(config.CanonicalModels[0].Streaming);
        Assert.True(config.CanonicalModels[0].Tools);
        Assert.False(config.CanonicalModels[0].Vision);
    }

    [Fact]
    public void ConfigurationBinding_LoadsAliasesFromJson()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Aliases:llama-3.3-70b:Candidates:0"] = "groq/llama-3.3-70b",
                ["Synaxis:InferenceGateway:Aliases:llama-3.3-70b:Candidates:1"] = "nvidia/llama-3.3-70b",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Single(config!.Aliases);
        Assert.True(config.Aliases.ContainsKey("llama-3.3-70b"));
        Assert.Equal(2, config.Aliases["llama-3.3-70b"].Candidates.Count);
        Assert.Equal("groq/llama-3.3-70b", config.Aliases["llama-3.3-70b"].Candidates[0]);
    }

    [Fact]
    public void ConfigurationBinding_LoadsJwtSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "my-secret-key",
                ["Synaxis:InferenceGateway:JwtIssuer"] = "synaxis",
                ["Synaxis:InferenceGateway:JwtAudience"] = "synaxis-client",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("my-secret-key", config!.JwtSecret);
        Assert.Equal("synaxis", config.JwtIssuer);
        Assert.Equal("synaxis-client", config.JwtAudience);
    }

    [Fact]
    public void ConfigurationBinding_DefaultMaxRequestBodySize()
    {
        // Arrange & Act
        var config = new SynaxisConfiguration();

        // Assert
        Assert.Equal(31457280, config.MaxRequestBodySize);
    }

    [Fact]
    public void ConfigurationBinding_LoadsMaxRequestBodySize()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:MaxRequestBodySize"] = "10485760",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(10485760, config!.MaxRequestBodySize);
    }

    [Fact]
    public void ProviderConfig_DefaultsEnabledToTrue()
    {
        // Arrange & Act
        var config = new ProviderConfig();

        // Assert
        Assert.True(config.Enabled);
    }

    [Fact]
    public void ProviderConfig_CanBeDisabled()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Groq:Enabled"] = "false",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.False(config!.Providers["Groq"].Enabled);
    }

    [Fact]
    public void ConfigurationBinding_LoadsCloudflareProviderWithAccountId()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Cloudflare:Type"] = "Cloudflare",
                ["Synaxis:InferenceGateway:Providers:Cloudflare:AccountId"] = "test-account-id",
                ["Synaxis:InferenceGateway:Providers:Cloudflare:Key"] = "test-key",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("test-account-id", config!.Providers["Cloudflare"].AccountId);
    }

    [Fact]
    public void ConfigurationBinding_LoadsProviderWithEndpoint()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:DeepSeek:Type"] = "OpenAI",
                ["Synaxis:InferenceGateway:Providers:DeepSeek:Endpoint"] = "https://api.deepseek.com/v1",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://api.deepseek.com/v1", config!.Providers["DeepSeek"].Endpoint);
    }

    [Fact]
    public void ConfigurationBinding_LoadsCanonicalModelCapabilities()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:CanonicalModels:0:Id"] = "gpt-4",
                ["Synaxis:InferenceGateway:CanonicalModels:0:Provider"] = "OpenAI",
                ["Synaxis:InferenceGateway:CanonicalModels:0:StructuredOutput"] = "true",
                ["Synaxis:InferenceGateway:CanonicalModels:0:LogProbs"] = "true",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.True(config!.CanonicalModels[0].StructuredOutput);
        Assert.True(config.CanonicalModels[0].LogProbs);
    }

    [Fact]
    public void ConfigurationBinding_EmptyConfiguration_ReturnsEmptyCollections()
    {
        // Arrange & Act
        var config = new SynaxisConfiguration();

        // Assert
        Assert.Empty(config.Providers);
        Assert.Empty(config.CanonicalModels);
        Assert.Empty(config.Aliases);
    }

    [Fact]
    public void AliasConfig_EmptyCandidatesList()
    {
        // Arrange & Act
        var alias = new AliasConfig();

        // Assert
        Assert.Empty(alias.Candidates);
    }

    [Fact]
    public void CanonicalModelConfig_Defaults()
    {
        // Arrange & Act
        var model = new CanonicalModelConfig();

        // Assert
        Assert.Empty(model.Id);
        Assert.Empty(model.Provider);
        Assert.Empty(model.ModelPath);
        Assert.False(model.Streaming);
        Assert.False(model.Tools);
        Assert.False(model.Vision);
        Assert.False(model.StructuredOutput);
        Assert.False(model.LogProbs);
    }

    #region Environment Variable Mapping Tests

    [Fact]
    public void EnvironmentVariableMapping_GroqApiKey_OverridesJson()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Groq:Key"] = "json-key",
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Groq:Key"] = "env-key",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("env-key", config!.Providers["Groq"].Key);
    }

    [Fact]
    public void EnvironmentVariableMapping_CloudflareAccountId_OverridesJson()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Cloudflare:AccountId"] = "json-account-id",
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Cloudflare:AccountId"] = "env-account-id",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("env-account-id", config!.Providers["Cloudflare"].AccountId);
    }

    [Fact]
    public void EnvironmentVariableMapping_MultipleProviders_AllMappedCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Groq:Key"] = "groq-env-key",
                ["Synaxis:InferenceGateway:Providers:Cohere:Key"] = "cohere-env-key",
                ["Synaxis:InferenceGateway:Providers:Gemini:Key"] = "gemini-env-key",
                ["Synaxis:InferenceGateway:Providers:OpenRouter:Key"] = "openrouter-env-key",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("groq-env-key", config!.Providers["Groq"].Key);
        Assert.Equal("cohere-env-key", config.Providers["Cohere"].Key);
        Assert.Equal("gemini-env-key", config.Providers["Gemini"].Key);
        Assert.Equal("openrouter-env-key", config.Providers["OpenRouter"].Key);
    }

    [Fact]
    public void EnvironmentVariableMapping_JwtSecret_OverridesJson()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "json-secret",
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "env-secret",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("env-secret", config!.JwtSecret);
    }

    [Fact]
    public void EnvironmentVariableMapping_NullValue_DoesNotOverride()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Groq:Key"] = "json-key",
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Groq:Key"] = null,
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("json-key", config!.Providers["Groq"].Key);
    }

    #endregion

    #region MasterKey Configuration Tests

    [Fact]
    public void ConfigurationBinding_LoadsMasterKey()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:MasterKey"] = "test-master-key",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("test-master-key", config!.MasterKey);
    }

    [Fact]
    public void ConfigurationBinding_MasterKeyDefaultsToNull()
    {
        // Arrange & Act
        var config = new SynaxisConfiguration();

        // Assert
        Assert.Null(config.MasterKey);
    }

    #endregion

    #region AntigravitySettings Tests

    [Fact]
    public void ConfigurationBinding_LoadsAntigravitySettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Antigravity:ClientId"] = "test-client-id",
                ["Synaxis:InferenceGateway:Antigravity:ClientSecret"] = "test-client-secret",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config!.Antigravity);
        Assert.Equal("test-client-id", config.Antigravity.ClientId);
        Assert.Equal("test-client-secret", config.Antigravity.ClientSecret);
    }

    [Fact]
    public void AntigravitySettings_Defaults()
    {
        // Arrange & Act
        var settings = new AntigravitySettings();

        // Assert
        Assert.Empty(settings.ClientId);
        Assert.Empty(settings.ClientSecret);
    }

    #endregion

    #region Provider Configuration Extended Tests

    [Fact]
    public void ProviderConfig_LoadsProjectId()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Antigravity:ProjectId"] = "test-project-id",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("test-project-id", config!.Providers["Antigravity"].ProjectId);
    }

    [Fact]
    public void ProviderConfig_LoadsAuthStoragePath()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Antigravity:AuthStoragePath"] = "/path/to/auth",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("/path/to/auth", config!.Providers["Antigravity"].AuthStoragePath);
    }

    [Fact]
    public void ProviderConfig_LoadsFallbackEndpoint()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Antigravity:FallbackEndpoint"] = "https://fallback.example.com/v1",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://fallback.example.com/v1", config!.Providers["Antigravity"].FallbackEndpoint);
    }

    [Fact]
    public void ProviderConfig_Defaults()
    {
        // Arrange & Act
        var config = new ProviderConfig();

        // Assert
        Assert.True(config.Enabled);
        Assert.Null(config.Key);
        Assert.Null(config.AccountId);
        Assert.Null(config.ProjectId);
        Assert.Null(config.AuthStoragePath);
        Assert.Equal(0, config.Tier);
        Assert.Empty(config.Models);
        Assert.Empty(config.Type);
        Assert.Null(config.Endpoint);
        Assert.Null(config.FallbackEndpoint);
    }

    [Fact]
    public void ProviderConfig_MultipleModels_LoadsCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Groq:Models:0"] = "llama-3.1-70b",
                ["Synaxis:InferenceGateway:Providers:Groq:Models:1"] = "llama-3.1-8b",
                ["Synaxis:InferenceGateway:Providers:Groq:Models:2"] = "mixtral-8x7b",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(3, config!.Providers["Groq"].Models.Count);
        Assert.Equal("llama-3.1-70b", config.Providers["Groq"].Models[0]);
        Assert.Equal("llama-3.1-8b", config.Providers["Groq"].Models[1]);
        Assert.Equal("mixtral-8x7b", config.Providers["Groq"].Models[2]);
    }

    [Fact]
    public void ProviderConfig_DifferentTiers_LoadsCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Providers:Groq:Tier"] = "1",
                ["Synaxis:InferenceGateway:Providers:OpenRouter:Tier"] = "2",
                ["Synaxis:InferenceGateway:Providers:DeepSeek:Tier"] = "3",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(1, config!.Providers["Groq"].Tier);
        Assert.Equal(2, config.Providers["OpenRouter"].Tier);
        Assert.Equal(3, config.Providers["DeepSeek"].Tier);
    }

    #endregion

    #region Canonical Model Configuration Extended Tests

    [Fact]
    public void CanonicalModelConfig_MultipleModels_LoadsCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:CanonicalModels:0:Id"] = "groq/llama-3.3-70b",
                ["Synaxis:InferenceGateway:CanonicalModels:0:Provider"] = "Groq",
                ["Synaxis:InferenceGateway:CanonicalModels:0:ModelPath"] = "llama-3.3-70b-versatile",
                ["Synaxis:InferenceGateway:CanonicalModels:1:Id"] = "nvidia/llama-3.3-70b",
                ["Synaxis:InferenceGateway:CanonicalModels:1:Provider"] = "NVIDIA",
                ["Synaxis:InferenceGateway:CanonicalModels:1:ModelPath"] = "meta/llama-3.3-70b-instruct",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(2, config!.CanonicalModels.Count);
        Assert.Equal("groq/llama-3.3-70b", config.CanonicalModels[0].Id);
        Assert.Equal("nvidia/llama-3.3-70b", config.CanonicalModels[1].Id);
    }

    [Fact]
    public void CanonicalModelConfig_AllCapabilities_LoadsCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:CanonicalModels:0:Id"] = "gpt-4",
                ["Synaxis:InferenceGateway:CanonicalModels:0:Provider"] = "OpenAI",
                ["Synaxis:InferenceGateway:CanonicalModels:0:ModelPath"] = "gpt-4",
                ["Synaxis:InferenceGateway:CanonicalModels:0:Streaming"] = "true",
                ["Synaxis:InferenceGateway:CanonicalModels:0:Tools"] = "true",
                ["Synaxis:InferenceGateway:CanonicalModels:0:Vision"] = "true",
                ["Synaxis:InferenceGateway:CanonicalModels:0:StructuredOutput"] = "true",
                ["Synaxis:InferenceGateway:CanonicalModels:0:LogProbs"] = "true",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.True(config!.CanonicalModels[0].Streaming);
        Assert.True(config.CanonicalModels[0].Tools);
        Assert.True(config.CanonicalModels[0].Vision);
        Assert.True(config.CanonicalModels[0].StructuredOutput);
        Assert.True(config.CanonicalModels[0].LogProbs);
    }

    #endregion

    #region Alias Configuration Extended Tests

    [Fact]
    public void AliasConfig_MultipleAliases_LoadsCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Aliases:llama-3.3-70b:Candidates:0"] = "groq/llama-3.3-70b",
                ["Synaxis:InferenceGateway:Aliases:llama-3.3-70b:Candidates:1"] = "nvidia/llama-3.3-70b",
                ["Synaxis:InferenceGateway:Aliases:llama-3.1-8b:Candidates:0"] = "groq/llama-3.1-8b",
                ["Synaxis:InferenceGateway:Aliases:llama-3.1-8b:Candidates:1"] = "cloudflare/llama-3.1-8b",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(2, config!.Aliases.Count);
        Assert.Equal(2, config.Aliases["llama-3.3-70b"].Candidates.Count);
        Assert.Equal(2, config.Aliases["llama-3.1-8b"].Candidates.Count);
    }

    [Fact]
    public void AliasConfig_EmptyCandidates_LoadsCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Aliases:phi-3-medium:Candidates:0"] = "",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.True(config!.Aliases.ContainsKey("phi-3-medium"));
        Assert.Empty(config.Aliases["phi-3-medium"].Candidates);
    }

    [Fact]
    public void AliasConfig_MultipleCandidates_LoadsCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaxis:InferenceGateway:Aliases:miser-intelligence:Candidates:0"] = "sambanova/llama-405b",
                ["Synaxis:InferenceGateway:Aliases:miser-intelligence:Candidates:1"] = "github/gpt-4o",
                ["Synaxis:InferenceGateway:Aliases:miser-intelligence:Candidates:2"] = "hyperbolic/llama-405b",
            })
            .Build();

        // Act
        var config = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(3, config!.Aliases["miser-intelligence"].Candidates.Count);
        Assert.Equal("sambanova/llama-405b", config.Aliases["miser-intelligence"].Candidates[0]);
        Assert.Equal("github/gpt-4o", config.Aliases["miser-intelligence"].Candidates[1]);
        Assert.Equal("hyperbolic/llama-405b", config.Aliases["miser-intelligence"].Candidates[2]);
    }

    #endregion
}

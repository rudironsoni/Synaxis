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
}

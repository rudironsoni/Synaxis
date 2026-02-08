namespace Synaxis.Contracts.Tests.V1.Auth;

using FluentAssertions;
using Synaxis.Contracts.V1.Auth;

public class ProviderCredentialsTests
{
    [Fact]
    public void ProviderCredentials_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var credentials = new ProviderCredentials
        {
            ProviderName = "OpenAI",
            CredentialType = "ApiKey",
            CredentialValue = "sk-test-123",
        };

        // Assert
        credentials.ProviderName.Should().Be("OpenAI");
        credentials.CredentialType.Should().Be("ApiKey");
        credentials.CredentialValue.Should().Be("sk-test-123");
    }

    [Fact]
    public void ProviderCredentials_DefaultValues_AreEmptyStrings()
    {
        // Arrange & Act
        var credentials = new ProviderCredentials();

        // Assert
        credentials.ProviderName.Should().BeEmpty();
        credentials.CredentialType.Should().BeEmpty();
        credentials.CredentialValue.Should().BeEmpty();
    }

    [Fact]
    public void ProviderCredentials_IsImmutable_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var credentials = new ProviderCredentials
        {
            ProviderName = "Anthropic",
            CredentialType = "OAuth",
            CredentialValue = "token-abc",
        };

        // Act & Assert
        credentials.ProviderName.Should().Be("Anthropic");
        credentials.CredentialType.Should().Be("OAuth");
        credentials.CredentialValue.Should().Be("token-abc");
    }

    [Fact]
    public void ProviderCredentials_WithDifferentCredentialTypes_CanBeCreated()
    {
        // Arrange & Act
        var apiKeyCredentials = new ProviderCredentials
        {
            ProviderName = "OpenAI",
            CredentialType = "ApiKey",
        };

        var oauthCredentials = new ProviderCredentials
        {
            ProviderName = "Google",
            CredentialType = "OAuth",
        };

        var serviceAccountCredentials = new ProviderCredentials
        {
            ProviderName = "Azure",
            CredentialType = "ServiceAccount",
        };

        // Assert
        apiKeyCredentials.CredentialType.Should().Be("ApiKey");
        oauthCredentials.CredentialType.Should().Be("OAuth");
        serviceAccountCredentials.CredentialType.Should().Be("ServiceAccount");
    }

    [Fact]
    public void ProviderCredentials_WithLongValue_HandlesLargeStrings()
    {
        // Arrange
        var longValue = new string('A', 1000);

        // Act
        var credentials = new ProviderCredentials
        {
            ProviderName = "Provider",
            CredentialType = "ApiKey",
            CredentialValue = longValue,
        };

        // Assert
        credentials.CredentialValue.Should().HaveLength(1000);
    }
}

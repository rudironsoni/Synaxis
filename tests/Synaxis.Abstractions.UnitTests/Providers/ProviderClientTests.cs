namespace Synaxis.Abstractions.Tests.Providers;

using FluentAssertions;
using Synaxis.Abstractions.Providers;

public class ProviderClientTests
{
    [Fact]
    public void IProviderClient_CanBeImplemented()
    {
        // Arrange & Act
        var provider = new TestProviderClient();

        // Assert
        provider.Should().BeAssignableTo<IProviderClient>();
        provider.ProviderName.Should().Be("test-provider");
    }

    [Fact]
    public void IProviderClient_ProviderName_IsRequired()
    {
        // Arrange & Act
        var provider = new TestProviderClient();

        // Assert
        provider.ProviderName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void IProviderClient_DifferentProviders_HaveDifferentNames()
    {
        // Arrange
        var provider1 = new TestProviderClient();
        var provider2 = new AnotherProviderClient();

        // Act & Assert
        provider1.ProviderName.Should().NotBe(provider2.ProviderName);
    }

    private sealed class TestProviderClient : IProviderClient
    {
        public string ProviderName => "test-provider";
    }

    private sealed class AnotherProviderClient : IProviderClient
    {
        public string ProviderName => "another-provider";
    }
}

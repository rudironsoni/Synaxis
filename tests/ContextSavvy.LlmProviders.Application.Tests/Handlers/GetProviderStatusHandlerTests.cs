using ContextSavvy.LlmProviders.Application.Dtos;
using ContextSavvy.LlmProviders.Application.Handlers;
using ContextSavvy.LlmProviders.Application.Queries;
using FluentAssertions;

namespace ContextSavvy.LlmProviders.Application.Tests.Handlers;

public class GetProviderStatusHandlerTests
{
    private readonly GetProviderStatusHandler _handler;

    public GetProviderStatusHandlerTests()
    {
        _handler = new GetProviderStatusHandler();
    }

    [Fact]
    public async Task Handle_ReturnsCorrectProviderStatusDto()
    {
        // Arrange
        var providerName = "OpenAI";
        var query = new GetProviderStatusQuery(providerName);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ProviderName.Should().Be(providerName);
        result.IsHealthy.Should().BeTrue();
        result.StatusMessage.Should().Be("Healthy and active");
        result.LastChecked.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_WithNoProviders_ShouldHandleGracefully()
    {
        // NOTE: Current implementation is mocked and doesn't actually check for provider existence.
        // This test documents current behavior which always returns a healthy status.
        
        // Arrange
        var query = new GetProviderStatusQuery("UnknownProvider");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProviderName.Should().Be("UnknownProvider");
    }
}

using ContextSavvy.LlmProviders.Application.Commands;
using ContextSavvy.LlmProviders.Application.Handlers;
using FluentAssertions;

namespace ContextSavvy.LlmProviders.Application.Tests.Handlers;

public class InitializeProviderHandlerTests
{
    private readonly InitializeProviderHandler _handler;

    public InitializeProviderHandlerTests()
    {
        _handler = new InitializeProviderHandler();
    }

    [Fact]
    public async Task Handle_ReturnsTrue()
    {
        // Arrange
        var command = new InitializeProviderCommand("OpenAI", false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }
}

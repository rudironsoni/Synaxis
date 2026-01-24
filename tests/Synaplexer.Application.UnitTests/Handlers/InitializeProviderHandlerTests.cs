using Synaplexer.Application.Commands;
using Synaplexer.Application.Handlers;
using FluentAssertions;

namespace Synaplexer.Application.Tests.Handlers;

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

using Synaplexer.Application.Dtos;
using Synaplexer.Application.Handlers;
using Synaplexer.Application.Queries;
using FluentAssertions;

namespace Synaplexer.Application.Tests.Handlers;

public class ListAvailableModelsHandlerTests
{
    private readonly ListAvailableModelsHandler _handler;

    public ListAvailableModelsHandlerTests()
    {
        _handler = new ListAvailableModelsHandler();
    }

    [Fact]
    public async Task Handle_ReturnsModelList()
    {
        // Arrange
        var query = new ListAvailableModelsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(m => m.Id == "gpt-4");
        result.Should().Contain(m => m.Id == "claude-3-opus");
    }
}

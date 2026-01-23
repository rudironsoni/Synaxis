using ContextSavvy.LlmProviders.Application.Commands;
using ContextSavvy.LlmProviders.Application.Dtos;
using ContextSavvy.LlmProviders.Application.Handlers;
using ContextSavvy.LlmProviders.Application.Services;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ContextSavvy.LlmProviders.Application.Tests.Handlers;

public class ChatCompletionHandlerTests
{
    private readonly ITieredProviderRouter _router;
    private readonly ChatCompletionHandler _handler;

    public ChatCompletionHandlerTests()
    {
        _router = Substitute.For<ITieredProviderRouter>();
        _handler = new ChatCompletionHandler(_router);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsChatCompletionResult()
    {
        // Arrange
        var command = new ChatCompletionCommand("gpt-4", [new ChatMessage("user", "Hello")], 0.7f, 2048, false);
        var expectedResult = new ChatCompletionResult("Hello back", "gpt-4", 100);
        _router.RouteAsync(command, Arg.Any<CancellationToken>()).Returns(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task Handle_CallsRouterRouteAsync()
    {
        // Arrange
        var command = new ChatCompletionCommand("gpt-4", [new ChatMessage("user", "Hello")]);
        
        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _router.Received(1).RouteAsync(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRouterThrows_PropagatesException()
    {
        // Arrange
        var command = new ChatCompletionCommand("gpt-4", [new ChatMessage("user", "Hello")]);
        _router.RouteAsync(command, Arg.Any<CancellationToken>()).Throws(new Exception("Router error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None).AsTask())
            .Should().ThrowAsync<Exception>().WithMessage("Router error");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_PassesStreamingPreferenceToRouter(bool stream)
    {
        // Arrange
        var command = new ChatCompletionCommand("gpt-4", [new ChatMessage("user", "Hello")], Stream: stream);
        
        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _router.Received(1).RouteAsync(Arg.Is<ChatCompletionCommand>(c => c.Stream == stream), Arg.Any<CancellationToken>());
    }
}

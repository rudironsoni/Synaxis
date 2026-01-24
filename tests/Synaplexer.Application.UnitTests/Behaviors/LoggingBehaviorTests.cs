using Synaplexer.Application.Behaviors;
using FluentAssertions;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Synaplexer.Application.Tests.Behaviors;

public class LoggingBehaviorTests
{
    private readonly ILogger<LoggingBehavior<IMessage, object>> _logger;
    private readonly LoggingBehavior<IMessage, object> _behavior;

    public LoggingBehaviorTests()
    {
        _logger = Substitute.For<ILogger<LoggingBehavior<IMessage, object>>>();
        _behavior = new LoggingBehavior<IMessage, object>(_logger);
    }

    [Fact]
    public async Task Handle_LogsRequestStartAndEnd()
    {
        // Arrange
        var message = Substitute.For<IMessage>();
        var response = new object();
        MessageHandlerDelegate<IMessage, object> next = (m, ct) => new ValueTask<object>(response);

        // Act
        var result = await _behavior.Handle(message, next, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        
        // Verify start log
        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Processing")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        // Verify end log
        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("handled successfully")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_LogsDuration()
    {
        // Arrange
        var message = Substitute.For<IMessage>();
        var response = new object();
        MessageHandlerDelegate<IMessage, object> next = async (m, ct) => 
        {
            await Task.Delay(10);
            return response;
        };

        // Act
        await _behavior.Handle(message, next, CancellationToken.None);

        // Assert
        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("ms")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_LogsErrorWithDuration()
    {
        // Arrange
        var message = Substitute.For<IMessage>();
        var exception = new Exception("Test failure");
        MessageHandlerDelegate<IMessage, object> next = (m, ct) => throw exception;

        // Act & Assert
        await _behavior.Invoking(b => b.Handle(message, next, CancellationToken.None).AsTask())
            .Should().ThrowAsync<Exception>();

        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("failed after")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }
}

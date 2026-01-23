using ContextSavvy.LlmProviders.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Mediator;
using NSubstitute;

namespace ContextSavvy.LlmProviders.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_PassesThrough()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<IMessage>>();
        var behavior = new ValidationBehavior<IMessage, object>(validators);
        var message = Substitute.For<IMessage>();
        var response = new object();
        MessageHandlerDelegate<IMessage, object> next = (m, ct) => new ValueTask<object>(response);

        // Act
        var result = await behavior.Handle(message, next, CancellationToken.None);

        // Assert
        result.Should().Be(response);
    }

    [Fact]
    public async Task Handle_WithValidRequest_PassesThrough()
    {
        // Arrange
        var validator = Substitute.For<IValidator<IMessage>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<IMessage>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        var validators = new[] { validator };
        var behavior = new ValidationBehavior<IMessage, object>(validators);
        var message = Substitute.For<IMessage>();
        var response = new object();
        MessageHandlerDelegate<IMessage, object> next = (m, ct) => new ValueTask<object>(response);

        // Act
        var result = await behavior.Handle(message, next, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        await validator.Received(1).ValidateAsync(Arg.Any<ValidationContext<IMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var validator = Substitute.For<IValidator<IMessage>>();
        var failures = new List<ValidationFailure> { new("Property", "Error message") };
        validator.ValidateAsync(Arg.Any<ValidationContext<IMessage>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));
        
        var validators = new[] { validator };
        var behavior = new ValidationBehavior<IMessage, object>(validators);
        var message = Substitute.For<IMessage>();
        MessageHandlerDelegate<IMessage, object> next = (m, ct) => new ValueTask<object>(new object());

        // Act & Assert
        await behavior.Invoking(b => b.Handle(message, next, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Count() == 1);
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_AggregatesFailures()
    {
        // Arrange
        var v1 = Substitute.For<IValidator<IMessage>>();
        v1.ValidateAsync(Arg.Any<ValidationContext<IMessage>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("P1", "E1") }));

        var v2 = Substitute.For<IValidator<IMessage>>();
        v2.ValidateAsync(Arg.Any<ValidationContext<IMessage>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("P2", "E2") }));

        var behavior = new ValidationBehavior<IMessage, object>(new[] { v1, v2 });
        var message = Substitute.For<IMessage>();
        MessageHandlerDelegate<IMessage, object> next = (m, ct) => new ValueTask<object>(new object());

        // Act & Assert
        await behavior.Invoking(b => b.Handle(message, next, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Count() == 2);
    }
}

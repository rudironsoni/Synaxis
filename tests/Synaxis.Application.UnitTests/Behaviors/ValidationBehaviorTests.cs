// <copyright file="ValidationBehaviorTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Tests.Behaviors
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Mediator;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using Synaxis.Behaviors;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="ValidationBehavior{TMessage, TResponse}"/>.
    /// </summary>
    public class ValidationBehaviorTests
    {
        private readonly ILogger<ValidationBehavior<TestMessage, TestResponse>> _logger;
        private readonly ValidationBehavior<TestMessage, TestResponse> _behavior;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationBehaviorTests"/> class.
        /// </summary>
        public ValidationBehaviorTests()
        {
            this._logger = Substitute.For<ILogger<ValidationBehavior<TestMessage, TestResponse>>>();
            this._behavior = new ValidationBehavior<TestMessage, TestResponse>(this._logger);
        }

        [Fact]
        public async Task Handle_LogsDebugMessage_BeforeValidating()
        {
            // Arrange
            var message = new TestMessage();
            MessageHandlerDelegate<TestMessage, TestResponse> next = (msg, ct) => 
                ValueTask.FromResult(new TestResponse());

            // Act
            await this._behavior.Handle(message, next, CancellationToken.None);

            // Assert
            this._logger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Validating") && o.ToString()!.Contains("TestMessage")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task Handle_CallsNextDelegate_WithCorrectParameters()
        {
            // Arrange
            var message = new TestMessage();
            var expectedResponse = new TestResponse();
            var nextCalled = false;

            MessageHandlerDelegate<TestMessage, TestResponse> next = (msg, ct) =>
            {
                nextCalled = true;
                msg.Should().Be(message);
                return ValueTask.FromResult(expectedResponse);
            };

            // Act
            var result = await this._behavior.Handle(message, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.Should().Be(expectedResponse);
        }

        [Fact]
        public async Task Handle_WithValidMessage_ReturnsExpectedResponse()
        {
            // Arrange
            var message = new TestMessage();
            var expectedResponse = new TestResponse();
            MessageHandlerDelegate<TestMessage, TestResponse> next = (msg, ct) => 
                ValueTask.FromResult(expectedResponse);

            // Act
            var result = await this._behavior.Handle(message, next, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);
        }

        [Fact]
        public async Task Handle_WithNullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            MessageHandlerDelegate<TestMessage, TestResponse> next = (msg, ct) => 
                ValueTask.FromResult(new TestResponse());

            // Act
            Func<Task> act = async () => await this._behavior.Handle(null!, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("message");
        }

        [Fact]
        public async Task Handle_WithNullNext_ThrowsArgumentNullException()
        {
            // Arrange
            var message = new TestMessage();

            // Act
            Func<Task> act = async () => await this._behavior.Handle(message, null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("next");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new ValidationBehavior<TestMessage, TestResponse>(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public async Task Handle_WithCancellationToken_PropagatesToken()
        {
            // Arrange
            var message = new TestMessage();
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            CancellationToken receivedToken = default;

            MessageHandlerDelegate<TestMessage, TestResponse> next = (msg, ct) =>
            {
                receivedToken = ct;
                return ValueTask.FromResult(new TestResponse());
            };

            // Act
            await this._behavior.Handle(message, next, token);

            // Assert
            receivedToken.Should().Be(token);
        }

        [Fact]
        public async Task Handle_WhenNextThrows_PropagatesException()
        {
            // Arrange
            var message = new TestMessage();
            var exception = new InvalidOperationException("Validation failed");
            MessageHandlerDelegate<TestMessage, TestResponse> next = (msg, ct) => 
                throw exception;

            // Act
            Func<Task> act = async () => await this._behavior.Handle(message, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Validation failed");
        }

        public sealed record TestMessage : IMessage;

        public sealed record TestResponse;
    }
}

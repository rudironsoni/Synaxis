// <copyright file="MetricsBehaviorTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Tests.Behaviors
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Mediator;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using Synaxis.Behaviors;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="MetricsBehavior{TMessage, TResponse}"/>.
    /// </summary>
    public class MetricsBehaviorTests
    {
        private readonly ILogger<MetricsBehavior<TestMessage, TestResponse>> _logger;
        private readonly MetricsBehavior<TestMessage, TestResponse> _behavior;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsBehaviorTests"/> class.
        /// </summary>
        public MetricsBehaviorTests()
        {
            this._logger = Substitute.For<ILogger<MetricsBehavior<TestMessage, TestResponse>>>();
            this._behavior = new MetricsBehavior<TestMessage, TestResponse>(this._logger);
        }

        [Fact]
        public async Task Handle_LogsMetrics_AfterExecution()
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
                Arg.Is<object>(o => o.ToString()!.Contains("Metrics") && o.ToString()!.Contains("TestMessage")),
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
        public async Task Handle_WithActivity_SetsMessageTypeTag()
        {
            // Arrange
            using var activity = new Activity("TestActivity").Start();
            var message = new TestMessage();
            MessageHandlerDelegate<TestMessage, TestResponse> next = (msg, ct) => 
                ValueTask.FromResult(new TestResponse());

            // Act
            await this._behavior.Handle(message, next, CancellationToken.None);

            // Assert
            var tag = activity.GetTagItem("synaxis.message_type");
            tag.Should().Be("TestMessage");
        }

        [Fact]
        public async Task Handle_WhenNextThrows_SetsErrorTag()
        {
            // Arrange
            using var activity = new Activity("TestActivity").Start();
            var message = new TestMessage();
            var exception = new InvalidOperationException("Test error");
            MessageHandlerDelegate<TestMessage, TestResponse> next = (msg, ct) => 
                throw exception;

            // Act
            Func<Task> act = async () => await this._behavior.Handle(message, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            var errorTag = activity.GetTagItem("synaxis.error");
            errorTag.Should().Be(true);
        }

        [Fact]
        public async Task Handle_WhenNextThrows_PropagatesException()
        {
            // Arrange
            var message = new TestMessage();
            var exception = new InvalidOperationException("Test error");
            MessageHandlerDelegate<TestMessage, TestResponse> next = (msg, ct) => 
                throw exception;

            // Act
            Func<Task> act = async () => await this._behavior.Handle(message, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Test error");
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
            Action act = () => new MetricsBehavior<TestMessage, TestResponse>(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public async Task Handle_MeasuresExecutionTime_AndLogsIt()
        {
            // Arrange
            var message = new TestMessage();
            MessageHandlerDelegate<TestMessage, TestResponse> next = async (msg, ct) =>
            {
                await Task.Delay(10, ct);
                return new TestResponse();
            };

            // Act
            await this._behavior.Handle(message, next, CancellationToken.None);

            // Assert
            this._logger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Duration")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
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
        public async Task Handle_WithoutActivity_DoesNotThrow()
        {
            // Arrange
            Activity.Current = null;
            var message = new TestMessage();
            MessageHandlerDelegate<TestMessage, TestResponse> next = (msg, ct) => 
                ValueTask.FromResult(new TestResponse());

            // Act
            Func<Task> act = async () => await this._behavior.Handle(message, next, CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync();
        }

        public sealed record TestMessage : IMessage;

        public sealed record TestResponse;
    }
}

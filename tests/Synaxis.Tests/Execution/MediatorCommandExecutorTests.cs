// <copyright file="MediatorCommandExecutorTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Tests.Execution
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Mediator;
    using NSubstitute;
    using Synaxis.Abstractions.Commands;
    using Synaxis.Execution;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="MediatorCommandExecutor{TCommand, TResult}"/>.
    /// </summary>
    public class MediatorCommandExecutorTests
    {
        private readonly IMediator _mediator;
        private readonly MediatorCommandExecutor<TestCommand, TestResult> _executor;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediatorCommandExecutorTests"/> class.
        /// </summary>
        public MediatorCommandExecutorTests()
        {
            this._mediator = Substitute.For<IMediator>();
            this._executor = new MediatorCommandExecutor<TestCommand, TestResult>(this._mediator);
        }

        [Fact]
        public async Task ExecuteAsync_CallsMediator_WithCorrectCommand()
        {
            // Arrange
            var command = new TestCommand();
            var expectedResult = new TestResult { Value = "Success" };
            this._mediator.Send(command, Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult(expectedResult));

            // Act
            var result = await this._executor.ExecuteAsync(command, CancellationToken.None);

            // Assert
            await this._mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
            result.Should().Be(expectedResult);
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellationToken_PropagatesToken()
        {
            // Arrange
            var command = new TestCommand();
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            this._mediator.Send(command, token)
                .Returns(ValueTask.FromResult(new TestResult()));

            // Act
            await this._executor.ExecuteAsync(command, token);

            // Assert
            await this._mediator.Received(1).Send(command, token);
        }

        [Fact]
        public async Task ExecuteAsync_WithNullCommand_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await this._executor.ExecuteAsync(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("command");
        }

        [Fact]
        public void Constructor_WithNullMediator_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new MediatorCommandExecutor<TestCommand, TestResult>(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("mediator");
        }

        [Fact]
        public async Task ExecuteAsync_WhenMediatorThrows_PropagatesException()
        {
            // Arrange
            var command = new TestCommand();
            var exception = new InvalidOperationException("Mediator error");
            this._mediator.Send(command, Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromException<TestResult>(exception));

            // Act
            Func<Task> act = async () => await this._executor.ExecuteAsync(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Mediator error");
        }

        [Fact]
        public async Task ExecuteAsync_WithCancelledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            var command = new TestCommand();
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            this._mediator.Send(command, Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var token = callInfo.Arg<CancellationToken>();
                    token.ThrowIfCancellationRequested();
                    return ValueTask.FromResult(new TestResult());
                });

            // Act
            Func<Task> act = async () => await this._executor.ExecuteAsync(command, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsCorrectResult_WhenSuccessful()
        {
            // Arrange
            var command = new TestCommand();
            var expectedResult = new TestResult { Value = "Test Result" };
            this._mediator.Send(command, Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult(expectedResult));

            // Act
            var result = await this._executor.ExecuteAsync(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().Be("Test Result");
        }

        public sealed record TestCommand : Synaxis.Abstractions.Commands.ICommand<TestResult>, global::Mediator.IRequest<TestResult>;

        public sealed record TestResult
        {
            public string Value { get; init; } = string.Empty;
        }
    }
}

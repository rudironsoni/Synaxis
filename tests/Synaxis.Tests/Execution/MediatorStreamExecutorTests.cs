// <copyright file="MediatorStreamExecutorTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Tests.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Mediator;
    using NSubstitute;
    using Synaxis.Abstractions.Commands;
    using Synaxis.Execution;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="MediatorStreamExecutor{TRequest, TResult}"/>.
    /// </summary>
    public class MediatorStreamExecutorTests
    {
        private readonly IMediator _mediator;
        private readonly MediatorStreamExecutor<TestRequest, TestResult> _executor;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediatorStreamExecutorTests"/> class.
        /// </summary>
        public MediatorStreamExecutorTests()
        {
            this._mediator = Substitute.For<IMediator>();
            this._executor = new MediatorStreamExecutor<TestRequest, TestResult>(this._mediator);
        }

        [Fact]
        public async Task ExecuteStreamAsync_CallsMediator_WithCorrectRequest()
        {
            // Arrange
            var request = new TestRequest();
            var results = new[] { new TestResult { Value = "1" }, new TestResult { Value = "2" } };
            this._mediator.CreateStream(request, Arg.Any<CancellationToken>())
                .Returns(CreateAsyncEnumerable(results));

            // Act
            var stream = this._executor.ExecuteStreamAsync(request, CancellationToken.None);
            var resultList = await stream.ToListAsync();

            // Assert
            this._mediator.Received(1).CreateStream(request, Arg.Any<CancellationToken>());
            resultList.Should().HaveCount(2);
        }

        [Fact]
        public async Task ExecuteStreamAsync_WithCancellationToken_PropagatesToken()
        {
            // Arrange
            var request = new TestRequest();
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            var results = new[] { new TestResult { Value = "1" } };
            this._mediator.CreateStream(request, token)
                .Returns(CreateAsyncEnumerable(results));

            // Act
            var stream = this._executor.ExecuteStreamAsync(request, token);
            await stream.ToListAsync(token);

            // Assert
            this._mediator.Received(1).CreateStream(request, token);
        }

        [Fact]
        public void ExecuteStreamAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => this._executor.ExecuteStreamAsync(null!, CancellationToken.None);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("request");
        }

        [Fact]
        public void Constructor_WithNullMediator_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new MediatorStreamExecutor<TestRequest, TestResult>(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("mediator");
        }

        [Fact]
        public async Task ExecuteStreamAsync_ReturnsCorrectResults_WhenSuccessful()
        {
            // Arrange
            var request = new TestRequest();
            var results = new[]
            {
                new TestResult { Value = "First" },
                new TestResult { Value = "Second" },
                new TestResult { Value = "Third" },
            };
            this._mediator.CreateStream(request, Arg.Any<CancellationToken>())
                .Returns(CreateAsyncEnumerable(results));

            // Act
            var stream = this._executor.ExecuteStreamAsync(request, CancellationToken.None);
            var resultList = await stream.ToListAsync();

            // Assert
            resultList.Should().HaveCount(3);
            resultList[0].Value.Should().Be("First");
            resultList[1].Value.Should().Be("Second");
            resultList[2].Value.Should().Be("Third");
        }

        [Fact]
        public async Task ExecuteStreamAsync_WithEmptyStream_ReturnsEmptySequence()
        {
            // Arrange
            var request = new TestRequest();
            this._mediator.CreateStream(request, Arg.Any<CancellationToken>())
                .Returns(CreateAsyncEnumerable(Array.Empty<TestResult>()));

            // Act
            var stream = this._executor.ExecuteStreamAsync(request, CancellationToken.None);
            var resultList = await stream.ToListAsync();

            // Assert
            resultList.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteStreamAsync_WhenMediatorThrows_PropagatesException()
        {
            // Arrange
            var request = new TestRequest();
            this._mediator.CreateStream(request, Arg.Any<CancellationToken>())
                .Returns(_ => throw new InvalidOperationException("Stream error"));

            // Act
            Func<Task> act = async () =>
            {
                var stream = this._executor.ExecuteStreamAsync(request, CancellationToken.None);
                await foreach (var _ in stream)
                {
                    // Consume stream
                }
            };

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Stream error");
        }

        [Fact]
        public async Task ExecuteStreamAsync_WithCancelledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            var request = new TestRequest();
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            this._mediator.CreateStream(request, Arg.Any<CancellationToken>())
                .Returns(callInfo => CreateCancellableStream(callInfo.Arg<CancellationToken>()));

            // Act
            Func<Task> act = async () =>
            {
                var stream = this._executor.ExecuteStreamAsync(request, cts.Token);
                await foreach (var _ in stream.WithCancellation(cts.Token))
                {
                    // Consume stream
                }
            };

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await Task.Yield();
                yield return item;
            }
        }

        private static async IAsyncEnumerable<TestResult> CreateCancellableStream(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return new TestResult { Value = "Should not reach here" };
        }

        public sealed record TestRequest : Synaxis.Abstractions.Commands.IStreamRequest<TestResult>, global::Mediator.IStreamRequest<TestResult>;

        public sealed record TestResult
        {
            public string Value { get; init; } = string.Empty;
        }
    }
}

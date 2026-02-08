// <copyright file="EmbeddingCommandHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Tests.Handlers.Embeddings
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using Synaxis.Abstractions.Routing;
    using Synaxis.Commands.Embeddings;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Handlers.Embeddings;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="EmbeddingCommandHandler"/>.
    /// </summary>
    public class EmbeddingCommandHandlerTests
    {
        private readonly IProviderSelector _providerSelector;
        private readonly ILogger<EmbeddingCommandHandler> _logger;
        private readonly EmbeddingCommandHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddingCommandHandlerTests"/> class.
        /// </summary>
        public EmbeddingCommandHandlerTests()
        {
            this._providerSelector = Substitute.For<IProviderSelector>();
            this._logger = Substitute.For<ILogger<EmbeddingCommandHandler>>();
            this._handler = new EmbeddingCommandHandler(this._providerSelector, this._logger);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ReturnsResponse()
        {
            // Arrange
            var input = new[] { "Hello, world!" };
            var command = new EmbeddingCommand(input, "text-embedding-ada-002");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            var result = await this._handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_CallsProviderSelector_WithCorrectCommand()
        {
            // Arrange
            var input = new[] { "Test input" };
            var command = new EmbeddingCommand(input, "text-embedding-ada-002");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await this._handler.Handle(command, CancellationToken.None);

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(command, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithCancellationToken_PropagatesToken()
        {
            // Arrange
            var input = new[] { "Test input" };
            var command = new EmbeddingCommand(input, "text-embedding-ada-002");
            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            this._providerSelector.SelectProviderAsync(command, token)
                .Returns(Task.FromResult("openai"));

            // Act
            await this._handler.Handle(command, token);

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(command, token);
        }

        [Fact]
        public async Task Handle_WithNullCommand_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await this._handler.Handle(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("request");
        }

        [Fact]
        public void Constructor_WithNullProviderSelector_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new EmbeddingCommandHandler(null!, this._logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("providerSelector");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new EmbeddingCommandHandler(this._providerSelector, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public async Task Handle_WithProviderOverride_SelectsSpecifiedProvider()
        {
            // Arrange
            var input = new[] { "Test input" };
            var command = new EmbeddingCommand(input, "text-embedding-ada-002", Provider: "cohere");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("cohere"));

            // Act
            await this._handler.Handle(command, CancellationToken.None);

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(Arg.Is<EmbeddingCommand>(c => c.Provider == "cohere"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithMultipleInputs_ProcessesAllInputs()
        {
            // Arrange
            var input = new[] { "First input", "Second input", "Third input" };
            var command = new EmbeddingCommand(input, "text-embedding-ada-002");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            var result = await this._handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            await this._providerSelector.Received(1)
                .SelectProviderAsync(Arg.Is<EmbeddingCommand>(c => c.Input.Length == 3), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_LogsDebugMessage_BeforeProcessing()
        {
            // Arrange
            var input = new[] { "Test input" };
            var command = new EmbeddingCommand(input, "text-embedding-ada-002");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await this._handler.Handle(command, CancellationToken.None);

            // Assert
            this._logger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("text-embedding-ada-002")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task Handle_LogsInformationMessage_AfterProviderSelection()
        {
            // Arrange
            var input = new[] { "Test input" };
            var command = new EmbeddingCommand(input, "text-embedding-ada-002");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await this._handler.Handle(command, CancellationToken.None);

            // Assert
            this._logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("openai")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task Handle_WhenProviderSelectorThrows_PropagatesException()
        {
            // Arrange
            var input = new[] { "Test input" };
            var command = new EmbeddingCommand(input, "text-embedding-ada-002");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromException<string>(new InvalidOperationException("Provider not found")));

            // Act
            Func<Task> act = async () => await this._handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Provider not found");
        }

        [Fact]
        public async Task Handle_WithEmptyInput_StillCallsProviderSelector()
        {
            // Arrange
            var input = Array.Empty<string>();
            var command = new EmbeddingCommand(input, "text-embedding-ada-002");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await this._handler.Handle(command, CancellationToken.None);

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(command, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithCancelledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            var input = new[] { "Test input" };
            var command = new EmbeddingCommand(input, "text-embedding-ada-002");
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(async callInfo =>
                {
                    var token = callInfo.Arg<CancellationToken>();
                    token.ThrowIfCancellationRequested();
                    return await Task.FromResult("openai");
                });

            // Act
            Func<Task> act = async () => await this._handler.Handle(command, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}

// <copyright file="ChatStreamHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Tests.Handlers.Chat
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using Synaxis.Abstractions.Routing;
    using Synaxis.Commands.Chat;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Handlers.Chat;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="ChatStreamHandler"/>.
    /// </summary>
    public class ChatStreamHandlerTests
    {
        private readonly IProviderSelector _providerSelector;
        private readonly ILogger<ChatStreamHandler> _logger;
        private readonly ChatStreamHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatStreamHandlerTests"/> class.
        /// </summary>
        public ChatStreamHandlerTests()
        {
            this._providerSelector = Substitute.For<IProviderSelector>();
            this._logger = Substitute.For<ILogger<ChatStreamHandler>>();
            this._handler = new ChatStreamHandler(this._providerSelector, this._logger);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ReturnsStreamChunks()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Hello" },
            };
            var command = new ChatStreamCommand(messages, "gpt-4");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            var chunks = await this._handler.Handle(command, CancellationToken.None).ToListAsync();

            // Assert
            chunks.Should().NotBeEmpty();
            var firstChunk = chunks.First();
            firstChunk.Should().NotBeNull();
            firstChunk.Id.Should().NotBeNullOrEmpty();
            firstChunk.Model.Should().Be("gpt-4");
            firstChunk.Created.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Handle_CallsProviderSelector_WithCorrectCommand()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatStreamCommand(messages, "gpt-4");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await foreach (var _ in this._handler.Handle(command, CancellationToken.None))
            {
                // Consume stream
            }

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(command, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithCancellationToken_PropagatesToken()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatStreamCommand(messages, "gpt-4");
            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            this._providerSelector.SelectProviderAsync(command, token)
                .Returns(Task.FromResult("openai"));

            // Act
            await foreach (var _ in this._handler.Handle(command, token))
            {
                // Consume stream
            }

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(command, token);
        }

        [Fact]
        public async Task Handle_WithNullCommand_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () =>
            {
                await foreach (var _ in this._handler.Handle(null!, CancellationToken.None))
                {
                    // Consume stream
                }
            };

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("request");
        }

        [Fact]
        public void Constructor_WithNullProviderSelector_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new ChatStreamHandler(null!, this._logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("providerSelector");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new ChatStreamHandler(this._providerSelector, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public async Task Handle_WithProviderOverride_SelectsSpecifiedProvider()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatStreamCommand(messages, "gpt-4", Provider: "anthropic");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("anthropic"));

            // Act
            await foreach (var _ in this._handler.Handle(command, CancellationToken.None))
            {
                // Consume stream
            }

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(Arg.Is<ChatStreamCommand>(c => c.Provider == "anthropic"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithTemperature_PreservesTemperatureInCommand()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatStreamCommand(messages, "gpt-4", Temperature: 0.7);
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await foreach (var _ in this._handler.Handle(command, CancellationToken.None))
            {
                // Consume stream
            }

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(Arg.Is<ChatStreamCommand>(c => c.Temperature == 0.7), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithMaxTokens_PreservesMaxTokensInCommand()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatStreamCommand(messages, "gpt-4", MaxTokens: 100);
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await foreach (var _ in this._handler.Handle(command, CancellationToken.None))
            {
                // Consume stream
            }

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(Arg.Is<ChatStreamCommand>(c => c.MaxTokens == 100), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_LogsDebugMessage_BeforeProcessing()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatStreamCommand(messages, "gpt-4");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await foreach (var _ in this._handler.Handle(command, CancellationToken.None))
            {
                // Consume stream
            }

            // Assert
            this._logger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("gpt-4")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task Handle_LogsInformationMessage_AfterProviderSelection()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatStreamCommand(messages, "gpt-4");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await foreach (var _ in this._handler.Handle(command, CancellationToken.None))
            {
                // Consume stream
            }

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
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatStreamCommand(messages, "gpt-4");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromException<string>(new InvalidOperationException("Provider not found")));

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var _ in this._handler.Handle(command, CancellationToken.None))
                {
                    // Consume stream
                }
            };

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Provider not found");
        }

        [Fact]
        public async Task Handle_WithCancelledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatStreamCommand(messages, "gpt-4");
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
            Func<Task> act = async () =>
            {
                await foreach (var _ in this._handler.Handle(command, cts.Token))
                {
                    // Consume stream
                }
            };

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}

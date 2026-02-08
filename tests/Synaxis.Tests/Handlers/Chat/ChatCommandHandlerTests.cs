// <copyright file="ChatCommandHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Tests.Handlers.Chat
{
    using System;
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
    /// Unit tests for <see cref="ChatCommandHandler"/>.
    /// </summary>
    public class ChatCommandHandlerTests
    {
        private readonly IProviderSelector _providerSelector;
        private readonly ILogger<ChatCommandHandler> _logger;
        private readonly ChatCommandHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatCommandHandlerTests"/> class.
        /// </summary>
        public ChatCommandHandlerTests()
        {
            this._providerSelector = Substitute.For<IProviderSelector>();
            this._logger = Substitute.For<ILogger<ChatCommandHandler>>();
            this._handler = new ChatCommandHandler(this._providerSelector, this._logger);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ReturnsResponse()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Hello" },
            };
            var command = new ChatCommand(messages, "gpt-4");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            var result = await this._handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.Model.Should().Be("gpt-4");
            result.Created.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Handle_CallsProviderSelector_WithCorrectCommand()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatCommand(messages, "gpt-4");
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
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatCommand(messages, "gpt-4");
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
            Action act = () => new ChatCommandHandler(null!, this._logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("providerSelector");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new ChatCommandHandler(this._providerSelector, null!);

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
            var command = new ChatCommand(messages, "gpt-4", Provider: "anthropic");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("anthropic"));

            // Act
            await this._handler.Handle(command, CancellationToken.None);

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(Arg.Is<ChatCommand>(c => c.Provider == "anthropic"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithTemperature_PreservesTemperatureInCommand()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatCommand(messages, "gpt-4", Temperature: 0.7);
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await this._handler.Handle(command, CancellationToken.None);

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(Arg.Is<ChatCommand>(c => c.Temperature == 0.7), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithMaxTokens_PreservesMaxTokensInCommand()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatCommand(messages, "gpt-4", MaxTokens: 100);
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await this._handler.Handle(command, CancellationToken.None);

            // Assert
            await this._providerSelector.Received(1)
                .SelectProviderAsync(Arg.Is<ChatCommand>(c => c.MaxTokens == 100), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_LogsDebugMessage_BeforeProcessing()
        {
            // Arrange
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatCommand(messages, "gpt-4");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("openai"));

            // Act
            await this._handler.Handle(command, CancellationToken.None);

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
            var command = new ChatCommand(messages, "gpt-4");
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
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Test" },
            };
            var command = new ChatCommand(messages, "gpt-4");
            this._providerSelector.SelectProviderAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromException<string>(new InvalidOperationException("Provider not found")));

            // Act
            Func<Task> act = async () => await this._handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Provider not found");
        }
    }
}

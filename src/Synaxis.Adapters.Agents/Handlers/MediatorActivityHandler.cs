// <copyright file="MediatorActivityHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Agents.Handlers
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Execution;
    using Synaxis.Adapters.Agents.State;
    using Synaxis.Contracts.V1.Commands;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Activity handler that routes messages to Synaxis command executor.
    /// This is a base class that can be extended to integrate with Bot Framework or other messaging platforms.
    /// </summary>
    public class MediatorActivityHandler
    {
        private readonly ICommandExecutor<IChatCommand<ChatResponse>, ChatResponse> _executor;
        private readonly ILogger<MediatorActivityHandler> _logger;
        private readonly ConversationStateManager _stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediatorActivityHandler"/> class.
        /// </summary>
        /// <param name="executor">The command executor for processing chat commands.</param>
        /// <param name="stateManager">The conversation state manager.</param>
        /// <param name="logger">The logger instance.</param>
        public MediatorActivityHandler(
            ICommandExecutor<IChatCommand<ChatResponse>, ChatResponse> executor,
            ConversationStateManager stateManager,
            ILogger<MediatorActivityHandler> logger)
        {
            this._executor = executor!;
            this._stateManager = stateManager!;
            this._logger = logger!;
        }

        /// <summary>
        /// Handles an incoming message by routing it to Synaxis.
        /// </summary>
        /// <param name="conversationId">The unique identifier for the conversation.</param>
        /// <param name="userId">The unique identifier for the user.</param>
        /// <param name="messageText">The text content of the message.</param>
        /// <param name="model">The model to use for completion (optional, defaults to gpt-4).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The AI-generated response content.</returns>
        public async Task<string> HandleMessageAsync(
            string conversationId,
            string userId,
            string messageText,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                this._logger.LogWarning("Received empty message from user {UserId}", userId);
                return string.Empty;
            }

            this._logger.LogInformation("Processing message from user {UserId} in conversation {ConversationId}", userId, conversationId);

            try
            {
                // Get conversation history
                var history = await this._stateManager.GetConversationHistoryAsync(conversationId, cancellationToken).ConfigureAwait(false);

                // Build messages array with history + new message
                var messages = history
                    .Append(new ChatMessage { Role = "user", Content = messageText })
                    .ToArray();

                // Create command
                var command = new ChatCommandImpl
                {
                    Model = model ?? "gpt-4",
                    Messages = messages,
                    UserId = userId,
                };

                // Execute via Synaxis
                var response = await this._executor.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);

                // Save to conversation history
                await this._stateManager.AddMessageAsync(conversationId, new ChatMessage { Role = "user", Content = messageText }, cancellationToken).ConfigureAwait(false);

                var assistantMessage = response.Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
                await this._stateManager.AddMessageAsync(
                    conversationId,
                    new ChatMessage { Role = "assistant", Content = assistantMessage },
                    cancellationToken).ConfigureAwait(false);

                this._logger.LogInformation("Successfully processed message for user {UserId}", userId);
                return assistantMessage;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing message from user {UserId}", userId);
                return "Sorry, I encountered an error processing your message.";
            }
        }

        /// <summary>
        /// Clears the conversation history for a specific conversation.
        /// </summary>
        /// <param name="conversationId">The unique identifier for the conversation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task ClearConversationAsync(string conversationId, CancellationToken cancellationToken = default)
        {
            return this._stateManager.ClearConversationHistoryAsync(conversationId, cancellationToken);
        }

        // Internal concrete implementation for command
        private sealed class ChatCommandImpl : IChatCommand<ChatResponse>
        {
            public string Model { get; init; } = string.Empty;

            public ChatMessage[] Messages { get; init; } = Array.Empty<ChatMessage>();

            public string UserId { get; init; } = string.Empty;
        }
    }
}

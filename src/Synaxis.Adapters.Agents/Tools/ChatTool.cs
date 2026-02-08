// <copyright file="ChatTool.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Agents.Tools
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.Abstractions.Execution;
    using Synaxis.Contracts.V1.Commands;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Exposes Synaxis chat functionality as a tool for function calling.
    /// </summary>
    public class ChatTool
    {
        private readonly ICommandExecutor<IChatCommand<ChatResponse>, ChatResponse> _executor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatTool"/> class.
        /// </summary>
        /// <param name="executor">The command executor for processing chat commands.</param>
        public ChatTool(ICommandExecutor<IChatCommand<ChatResponse>, ChatResponse> executor)
        {
            this._executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        /// <summary>
        /// Generates a chat completion using the specified model and message.
        /// </summary>
        /// <param name="message">The user's message to process.</param>
        /// <param name="model">The AI model to use for the completion (default: gpt-4).</param>
        /// <param name="systemPrompt">Optional system prompt to guide the AI behavior.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The AI-generated response content.</returns>
        [Description("Generate chat completions using AI models")]
        public async Task<string> GetChatCompletionAsync(
            string message,
            string model = "gpt-4",
            string? systemPrompt = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));
            }

            var messages = new System.Collections.Generic.List<ChatMessage>();

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new ChatMessage { Role = "system", Content = systemPrompt });
            }

            messages.Add(new ChatMessage { Role = "user", Content = message });

            var command = new ChatCommandImpl
            {
                Model = model,
                Messages = messages.ToArray(),
            };

            var result = await this._executor.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);

            return result.Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }

        /// <summary>
        /// Generates a chat completion with conversation history.
        /// </summary>
        /// <param name="messages">The conversation history as a JSON-serialized array of messages.</param>
        /// <param name="model">The AI model to use for the completion (default: gpt-4).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The AI-generated response content.</returns>
        [Description("Generate chat completions using AI models with conversation history")]
        public async Task<string> GetChatCompletionWithHistoryAsync(
            string messages,
            string model = "gpt-4",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messages))
            {
                throw new ArgumentException("Messages cannot be null or empty.", nameof(messages));
            }

            // Deserialize the messages JSON
            var messageList = System.Text.Json.JsonSerializer.Deserialize<ChatMessage[]>(messages)
                ?? Array.Empty<ChatMessage>();

            var command = new ChatCommandImpl
            {
                Model = model,
                Messages = messageList,
            };

            var result = await this._executor.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);

            return result.Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }

        // Internal concrete implementation for command
        private sealed class ChatCommandImpl : IChatCommand<ChatResponse>
        {
            public string Model { get; init; } = string.Empty;

            public ChatMessage[] Messages { get; init; } = Array.Empty<ChatMessage>();
        }
    }
}

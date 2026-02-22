// <copyright file="AnthropicChatProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Anthropic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.Abstractions.Providers;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Implements the chat provider interface for Anthropic's Claude API.
    /// </summary>
    public sealed class AnthropicChatProvider : IChatProvider
    {
        private readonly AnthropicClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnthropicChatProvider"/> class.
        /// </summary>
        /// <param name="client">The Anthropic client.</param>
        public AnthropicChatProvider(AnthropicClient client)
        {
            this.client = client!;
        }

        /// <inheritdoc/>
        public string ProviderName => "anthropic";

        /// <inheritdoc/>
        public async Task<object> ChatAsync(
            IEnumerable<object> messages,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(messages);
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("Model cannot be null or empty.", nameof(model));
            }

            var chatMessages = messages.OfType<ChatMessage>().ToList();
            if (chatMessages.Count == 0)
            {
                throw new ArgumentException("Messages must contain at least one ChatMessage.", nameof(messages));
            }

            var chatOptions = this.ParseOptions(options);

            if (chatOptions.Stream)
            {
                return this.StreamChatAsync(chatMessages, model, chatOptions, cancellationToken);
            }

            return await this.client.ChatCompletionAsync(
                chatMessages,
                model,
                chatOptions.MaxTokens,
                chatOptions.Temperature,
                stream: false,
                cancellationToken).ConfigureAwait(false);
        }

        private async IAsyncEnumerable<string> StreamChatAsync(
            IEnumerable<ChatMessage> messages,
            string model,
            ChatOptions options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var chunk in this.client.ChatCompletionStreamAsync(
                messages,
                model,
                options.MaxTokens,
                options.Temperature,
                cancellationToken).ConfigureAwait(false))
            {
                yield return chunk;
            }
        }

        private ChatOptions ParseOptions(object? options)
        {
            if (options == null)
            {
                return new ChatOptions();
            }

            if (options is ChatOptions chatOptions)
            {
                return chatOptions;
            }

            // Attempt to extract properties from an anonymous object or dictionary
            var type = options.GetType();
            var maxTokensProperty = type.GetProperty("MaxTokens");
            var temperatureProperty = type.GetProperty("Temperature");
            var streamProperty = type.GetProperty("Stream");

            return new ChatOptions
            {
                MaxTokens = maxTokensProperty?.GetValue(options) as int?,
                Temperature = temperatureProperty?.GetValue(options) as double?,
                Stream = streamProperty?.GetValue(options) as bool? ?? false,
            };
        }

        /// <summary>
        /// Options for chat completion requests.
        /// </summary>
        private sealed class ChatOptions
        {
            /// <summary>
            /// Gets or sets the maximum number of tokens to generate.
            /// </summary>
            public int? MaxTokens { get; set; }

            /// <summary>
            /// Gets or sets the sampling temperature.
            /// </summary>
            public double? Temperature { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to stream the response.
            /// </summary>
            public bool Stream { get; set; }
        }
    }
}

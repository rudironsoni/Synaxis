// <copyright file="ChatRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Models
{
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Represents a chat completion request.
    /// </summary>
    public sealed class ChatRequest
    {
        /// <summary>
        /// Gets or initializes the messages array.
        /// </summary>
        public ChatMessage[] Messages { get; init; } = System.Array.Empty<ChatMessage>();

        /// <summary>
        /// Gets or initializes the model to use.
        /// </summary>
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the temperature.
        /// </summary>
        public double? Temperature { get; init; }

        /// <summary>
        /// Gets or initializes the maximum tokens.
        /// </summary>
        public int? MaxTokens { get; init; }

        /// <summary>
        /// Gets or initializes the top-p sampling value.
        /// </summary>
        public double? TopP { get; init; }

        /// <summary>
        /// Gets or initializes the frequency penalty.
        /// </summary>
        public double? FrequencyPenalty { get; init; }

        /// <summary>
        /// Gets or initializes the presence penalty.
        /// </summary>
        public double? PresencePenalty { get; init; }

        /// <summary>
        /// Gets or initializes the stop sequences.
        /// </summary>
        public string[]? Stop { get; init; }
    }
}

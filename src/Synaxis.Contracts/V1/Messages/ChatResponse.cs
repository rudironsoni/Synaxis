// <copyright file="ChatResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents a chat completion response with id, model, choices, and usage statistics.
    /// </summary>
    public sealed class ChatResponse
    {
        /// <summary>
        /// Gets or initializes the unique identifier for this completion.
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the object type (typically "chat.completion").
        /// </summary>
        public string Object { get; init; } = "chat.completion";

        /// <summary>
        /// Gets or initializes the Unix timestamp when this completion was created.
        /// </summary>
        public long Created { get; init; }

        /// <summary>
        /// Gets or initializes the model used for this completion.
        /// </summary>
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the list of completion choices.
        /// </summary>
        public ChatChoice[] Choices { get; init; } = System.Array.Empty<ChatChoice>();

        /// <summary>
        /// Gets or initializes the usage statistics for this completion.
        /// </summary>
        public ChatUsage? Usage { get; init; }
    }
}

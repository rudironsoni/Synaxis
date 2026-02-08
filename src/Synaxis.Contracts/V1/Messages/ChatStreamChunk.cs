// <copyright file="ChatStreamChunk.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents a chunk in a streamed chat completion response.
    /// </summary>
    public sealed class ChatStreamChunk
    {
        /// <summary>
        /// Gets or initializes the unique identifier for this completion.
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the object type (typically "chat.completion.chunk").
        /// </summary>
        public string Object { get; init; } = "chat.completion.chunk";

        /// <summary>
        /// Gets or initializes the Unix timestamp when this chunk was created.
        /// </summary>
        public long Created { get; init; }

        /// <summary>
        /// Gets or initializes the model used for this completion.
        /// </summary>
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the list of delta choices for this chunk.
        /// </summary>
        public ChatChoice[] Choices { get; init; } = System.Array.Empty<ChatChoice>();
    }
}

// <copyright file="ChatChoice.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents a single choice in a chat completion response.
    /// </summary>
    public sealed class ChatChoice
    {
        /// <summary>
        /// Gets or initializes the index of this choice in the list of choices.
        /// </summary>
        public int Index { get; init; }

        /// <summary>
        /// Gets or initializes the message content for this choice.
        /// </summary>
        public ChatMessage Message { get; init; } = new ChatMessage();

        /// <summary>
        /// Gets or initializes the reason why the model stopped generating tokens.
        /// </summary>
        public string? FinishReason { get; init; }
    }
}

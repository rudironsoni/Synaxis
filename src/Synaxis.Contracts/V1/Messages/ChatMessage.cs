// <copyright file="ChatMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    using System;

    /// <summary>
    /// Represents a chat message with role, content, and optional name.
    /// </summary>
    public sealed class ChatMessage
    {
        /// <summary>
        /// Gets or initializes the role of the message sender (e.g., "user", "assistant", "system").
        /// </summary>
        public string Role { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the content of the message.
        /// </summary>
        public string Content { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the optional name of the message sender.
        /// </summary>
        public string? Name { get; init; }
    }
}

// <copyright file="ConversationState.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Agents.State
{
    using System.Collections.Generic;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Represents the state of a conversation.
    /// </summary>
    public sealed class ConversationState
    {
        /// <summary>
        /// Gets or sets the list of messages in the conversation.
        /// </summary>
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}

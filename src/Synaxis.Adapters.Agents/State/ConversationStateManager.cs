// <copyright file="ConversationStateManager.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Agents.State
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Manages conversation state and history for Bot Framework conversations.
    /// </summary>
    public class ConversationStateManager
    {
        private readonly IConversationStorage _storage;
        private readonly int _maxHistoryMessages;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationStateManager"/> class.
        /// </summary>
        /// <param name="storage">The storage provider for persisting conversation state.</param>
        /// <param name="maxHistoryMessages">The maximum number of messages to keep in history (default: 20).</param>
        public ConversationStateManager(IConversationStorage storage, int maxHistoryMessages = 20)
        {
            this._storage = storage!;
            this._maxHistoryMessages = maxHistoryMessages > 0 ? maxHistoryMessages : 20;
        }

        /// <summary>
        /// Gets the conversation history for a specific conversation.
        /// </summary>
        /// <param name="conversationId">The unique identifier for the conversation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An array of chat messages representing the conversation history.</returns>
        public async Task<ChatMessage[]> GetConversationHistoryAsync(
            string conversationId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return Array.Empty<ChatMessage>();
            }

            var state = await this._storage.GetAsync(conversationId, cancellationToken).ConfigureAwait(false);
            return state?.Messages.ToArray() ?? Array.Empty<ChatMessage>();
        }

        /// <summary>
        /// Adds a message to the conversation history.
        /// </summary>
        /// <param name="conversationId">The unique identifier for the conversation.</param>
        /// <param name="message">The message to add to the history.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddMessageAsync(
            string conversationId,
            ChatMessage message,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty.", nameof(conversationId));
            }

            ArgumentNullException.ThrowIfNull(message);
            var state = await this._storage.GetAsync(conversationId, cancellationToken).ConfigureAwait(false)
                ?? new ConversationState { Messages = new System.Collections.Generic.List<ChatMessage>() };

            state.Messages.Add(message);

            // Trim history to max size
            if (state.Messages.Count > this._maxHistoryMessages)
            {
                state.Messages = state.Messages
                    .Skip(state.Messages.Count - this._maxHistoryMessages)
                    .ToList();
            }

            await this._storage.SetAsync(conversationId, state, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Clears the conversation history for a specific conversation.
        /// </summary>
        /// <param name="conversationId">The unique identifier for the conversation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClearConversationHistoryAsync(
            string conversationId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return;
            }

            await this._storage.DeleteAsync(conversationId, cancellationToken).ConfigureAwait(false);
        }
    }
}

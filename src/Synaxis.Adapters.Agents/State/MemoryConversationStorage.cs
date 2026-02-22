// <copyright file="MemoryConversationStorage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Agents.State
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// In-memory implementation of conversation storage for development and testing.
    /// </summary>
    public sealed class MemoryConversationStorage : IConversationStorage
    {
        private readonly ConcurrentDictionary<string, ConversationState> _storage = new ConcurrentDictionary<string, ConversationState>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public Task<ConversationState?> GetAsync(string conversationId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return Task.FromResult<ConversationState?>(null);
            }

            this._storage.TryGetValue(conversationId, out var state);
            return Task.FromResult(state);
        }

        /// <inheritdoc/>
        public Task SetAsync(string conversationId, ConversationState state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty.", nameof(conversationId));
            }

            ArgumentNullException.ThrowIfNull(state);
            this._storage[conversationId] = state;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string conversationId, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(conversationId))
            {
                this._storage.TryRemove(conversationId, out _);
            }

            return Task.CompletedTask;
        }
    }
}

// <copyright file="IConversationStorage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Agents.State
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for storing conversation state.
    /// </summary>
    public interface IConversationStorage
    {
        /// <summary>
        /// Gets the conversation state for a specific conversation.
        /// </summary>
        /// <param name="conversationId">The unique identifier for the conversation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The conversation state, or null if not found.</returns>
        Task<ConversationState?> GetAsync(string conversationId, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the conversation state for a specific conversation.
        /// </summary>
        /// <param name="conversationId">The unique identifier for the conversation.</param>
        /// <param name="state">The conversation state to store.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SetAsync(string conversationId, ConversationState state, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the conversation state for a specific conversation.
        /// </summary>
        /// <param name="conversationId">The unique identifier for the conversation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(string conversationId, CancellationToken cancellationToken);
    }
}

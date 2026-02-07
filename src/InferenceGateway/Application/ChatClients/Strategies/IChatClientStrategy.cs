// <copyright file="IChatClientStrategy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ChatClients.Strategies
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;

    /// <summary>
    /// Strategy interface for handling different chat client providers.
    /// </summary>
    public interface IChatClientStrategy
    {
        /// <summary>
        /// Determines if this strategy handles the given provider type (e.g. "Groq", "OpenAI").
        /// </summary>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if the strategy can handle this provider type.</returns>
        bool CanHandle(string providerType);

        /// <summary>
        /// Executes non-streaming request.
        /// </summary>
        /// <param name="client">The chat client.</param>
        /// <param name="messages">The chat messages.</param>
        /// <param name="options">The chat options.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The chat response.</returns>
        Task<ChatResponse> ExecuteAsync(
            IChatClient client,
            IEnumerable<ChatMessage> messages,
            ChatOptions options,
            CancellationToken ct);

        /// <summary>
        /// Executes streaming request.
        /// </summary>
        /// <param name="client">The chat client.</param>
        /// <param name="messages">The chat messages.</param>
        /// <param name="options">The chat options.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The chat response updates.</returns>
        IAsyncEnumerable<ChatResponseUpdate> ExecuteStreamingAsync(
            IChatClient client,
            IEnumerable<ChatMessage> messages,
            ChatOptions options,
            CancellationToken ct);
    }
}

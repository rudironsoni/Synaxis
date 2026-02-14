// <copyright file="IProviderAdapter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Providers.Models;

    /// <summary>
    /// Defines a contract for provider adapters that normalize different AI provider APIs.
    /// </summary>
    public interface IProviderAdapter
    {
        /// <summary>
        /// Gets the type of provider this adapter supports.
        /// </summary>
        ProviderType ProviderType { get; }

        /// <summary>
        /// Generates a chat completion asynchronously.
        /// </summary>
        /// <param name="request">The chat request.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the chat response.</returns>
        Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a streaming chat completion asynchronously.
        /// </summary>
        /// <param name="request">The chat request.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of streaming response chunks.</returns>
        IAsyncEnumerable<StreamingResponse> StreamChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates embeddings for the specified input texts asynchronously.
        /// </summary>
        /// <param name="request">The embedding request.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the embedding response.</returns>
        Task<EmbeddingResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken = default);
    }
}

// <copyright file="IRerankProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Providers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for providers that support document reranking.
    /// </summary>
    public interface IRerankProvider : IProviderClient
    {
        /// <summary>
        /// Reranks documents based on their relevance to a query asynchronously.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="documents">The documents to rerank.</param>
        /// <param name="model">The model to use for reranking.</param>
        /// <param name="options">Optional reranking parameters.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the reranking response.</returns>
        Task<object> RerankAsync(
            string query,
            IEnumerable<string> documents,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default);
    }
}

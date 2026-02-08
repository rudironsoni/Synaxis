// <copyright file="IEmbeddingProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Providers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for providers that support text embeddings.
    /// </summary>
    public interface IEmbeddingProvider : IProviderClient
    {
        /// <summary>
        /// Generates embeddings for the specified input texts asynchronously.
        /// </summary>
        /// <param name="inputs">The input texts to embed.</param>
        /// <param name="model">The model to use for embedding.</param>
        /// <param name="options">Optional embedding parameters.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the embedding response.</returns>
        Task<object> EmbedAsync(
            IEnumerable<string> inputs,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default);
    }
}

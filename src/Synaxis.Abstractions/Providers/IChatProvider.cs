// <copyright file="IChatProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Providers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for providers that support chat completions.
    /// </summary>
    public interface IChatProvider : IProviderClient
    {
        /// <summary>
        /// Generates a chat completion asynchronously.
        /// </summary>
        /// <param name="messages">The conversation messages.</param>
        /// <param name="model">The model to use for completion.</param>
        /// <param name="options">Optional completion parameters.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the chat response.</returns>
        Task<object> ChatAsync(
            IEnumerable<object> messages,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default);
    }
}

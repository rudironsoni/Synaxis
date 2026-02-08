// <copyright file="IStreamExecutor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Execution
{
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Defines a contract for executing streaming requests that produce multiple results.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to execute.</typeparam>
    /// <typeparam name="TResult">The type of result produced by the stream.</typeparam>
    public interface IStreamExecutor<in TRequest, out TResult>
    {
        /// <summary>
        /// Executes the specified streaming request asynchronously.
        /// </summary>
        /// <param name="request">The request to execute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TResult}"/> representing the streaming results.</returns>
        IAsyncEnumerable<TResult> ExecuteStreamAsync(TRequest request, CancellationToken cancellationToken);
    }
}

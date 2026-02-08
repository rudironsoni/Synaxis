// <copyright file="IProviderSelector.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Routing
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for selecting the appropriate provider for a request.
    /// </summary>
    public interface IProviderSelector
    {
        /// <summary>
        /// Selects the appropriate provider for the specified request asynchronously.
        /// </summary>
        /// <param name="request">The request for which to select a provider.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> containing the selected provider name.</returns>
        Task<string> SelectProviderAsync(object request, CancellationToken cancellationToken);
    }
}

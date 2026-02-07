// <copyright file="IHealthStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Stores and manages provider health state.
    /// </summary>
    public interface IHealthStore
    {
        /// <summary>
        /// Checks if a provider is healthy.
        /// </summary>
        /// <param name="providerKey">The provider key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if healthy, otherwise false.</returns>
        Task<bool> IsHealthyAsync(string providerKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a provider as failed with a cooldown period.
        /// </summary>
        /// <param name="providerKey">The provider key.</param>
        /// <param name="cooldown">The cooldown period.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MarkFailureAsync(string providerKey, TimeSpan cooldown, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a provider as successful.
        /// </summary>
        /// <param name="providerKey">The provider key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MarkSuccessAsync(string providerKey, CancellationToken cancellationToken = default);
    }
}

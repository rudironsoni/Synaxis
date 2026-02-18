// <copyright file="IServiceDiscoveryClient.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.Checks
{
    /// <summary>
    /// Interface for service discovery client.
    /// </summary>
    public interface IServiceDiscoveryClient
    {
        /// <summary>
        /// Checks if the registry is accessible.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if accessible, false otherwise.</returns>
        Task<bool> IsRegistryAccessibleAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of registered services.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of registered services.</returns>
        Task<int> GetRegisteredServicesCountAsync(CancellationToken cancellationToken = default);
    }
}

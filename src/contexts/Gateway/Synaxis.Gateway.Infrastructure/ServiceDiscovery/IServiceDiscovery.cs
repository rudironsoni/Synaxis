// <copyright file="IServiceDiscovery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Infrastructure.ServiceDiscovery;

/// <summary>
/// Defines a contract for service discovery operations.
/// </summary>
public interface IServiceDiscovery
{
    /// <summary>
    /// Registers a service instance with the discovery system.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="instance">The service instance to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RegisterAsync(
        string serviceName,
        ServiceInstance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters a service instance.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeregisterAsync(
        string serviceName,
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers healthy instances of a service.
    /// </summary>
    /// <param name="serviceName">The name of the service to discover.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of healthy service instances.</returns>
    Task<IReadOnlyList<ServiceInstance>> DiscoverAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single healthy instance using load balancing.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A healthy service instance, or null if none available.</returns>
    Task<ServiceInstance?> GetInstanceAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the health status of a service instance.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="isHealthy">Whether the instance is healthy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateHealthAsync(
        string serviceName,
        string instanceId,
        bool isHealthy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches for changes to a service's instances.
    /// </summary>
    /// <param name="serviceName">The name of the service to watch.</param>
    /// <param name="onChange">Callback invoked when instances change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the watch operation.</returns>
    Task WatchAsync(
        string serviceName,
        Func<IReadOnlyList<ServiceInstance>, Task> onChange,
        CancellationToken cancellationToken = default);
}

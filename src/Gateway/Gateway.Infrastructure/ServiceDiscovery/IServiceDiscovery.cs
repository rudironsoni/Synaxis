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

/// <summary>
/// Represents a discovered service instance.
/// </summary>
public class ServiceInstance
{
    /// <summary>
    /// Gets or sets the unique identifier of the instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the host address.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port number.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the protocol (http/https).
    /// </summary>
    public string Protocol { get; set; } = "http";

    /// <summary>
    /// Gets or sets the health check path.
    /// </summary>
    public string? HealthCheckPath { get; set; }

    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public HealthStatus Health { get; set; } = HealthStatus.Unknown;

    /// <summary>
    /// Gets or sets the last health check timestamp.
    /// </summary>
    public DateTime? LastHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets the metadata/tags.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the full address URL.
    /// </summary>
    public string Address => $"{this.Protocol}://{this.Host}:{this.Port}";

    /// <summary>
    /// Creates a new service instance.
    /// </summary>
    public static ServiceInstance Create(
        string serviceName,
        string host,
        int port,
        string protocol = "http",
        string? healthCheckPath = null,
        Dictionary<string, string>? metadata = null)
    {
        return new ServiceInstance
        {
            Id = Guid.NewGuid().ToString(),
            ServiceName = serviceName,
            Host = host,
            Port = port,
            Protocol = protocol,
            HealthCheckPath = healthCheckPath ?? "/health",
            Health = HealthStatus.Healthy,
            LastHealthCheck = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, string>(),
        };
    }
}

/// <summary>
/// Represents the health status of a service instance.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Health status is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Instance is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// Instance is unhealthy.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Instance is in a degraded state.
    /// </summary>
    Degraded,
}

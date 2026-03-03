// <copyright file="ServiceInstance.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Infrastructure.ServiceDiscovery;

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
    /// Gets or sets the metadata or tags.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the full address URL.
    /// </summary>
    public string Address => $"{this.Protocol}://{this.Host}:{this.Port}";

    /// <summary>
    /// Creates a new service instance.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="host">The host address.</param>
    /// <param name="port">The port number.</param>
    /// <param name="protocol">The protocol to use.</param>
    /// <param name="healthCheckPath">The health check path.</param>
    /// <param name="metadata">Additional metadata for the instance.</param>
    /// <returns>A configured <see cref="ServiceInstance"/>.</returns>
    public static ServiceInstance Create(
        string serviceName,
        string host,
        int port,
        string protocol = "http",
        string? healthCheckPath = null,
        IReadOnlyDictionary<string, string>? metadata = null)
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
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.Ordinal),
        };
    }
}

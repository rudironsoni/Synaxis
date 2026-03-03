// <copyright file="GatewayCluster.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Represents a gateway cluster.
/// </summary>
public class GatewayCluster
{
    /// <summary>
    /// Gets or sets the cluster identifier.
    /// </summary>
    public string ClusterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name for service discovery.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the destinations.
    /// </summary>
    public IList<DestinationConfig> Destinations { get; set; } = new List<DestinationConfig>();

    /// <summary>
    /// Gets or sets the load balancing policy.
    /// </summary>
    public string LoadBalancingPolicy { get; set; } = "RoundRobin";

    /// <summary>
    /// Gets or sets the health check options.
    /// </summary>
    public HealthCheckOptions HealthCheck { get; set; } = new();

    /// <summary>
    /// Gets or sets the circuit breaker options.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Gets or sets the retry policy.
    /// </summary>
    public RetryPolicyOptions RetryPolicy { get; set; } = new();
}

// <copyright file="GatewayRoute.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Represents a gateway route.
/// </summary>
public class GatewayRoute
{
    /// <summary>
    /// Gets or sets the route identifier.
    /// </summary>
    public string RouteId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the route pattern.
    /// </summary>
    public string RoutePattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP methods allowed.
    /// </summary>
    public IList<string> Methods { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the target cluster.
    /// </summary>
    public string ClusterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority (lower is higher priority).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets the rate limiting options for this route.
    /// </summary>
    public RateLimitOptions? RateLimiting { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the authentication is required.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Gets or sets the required scopes.
    /// </summary>
    public IList<string> RequiredScopes { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the request/response transforms.
    /// </summary>
    public IList<TransformConfig> Transforms { get; set; } = new List<TransformConfig>();

    /// <summary>
    /// Gets or sets the health check options.
    /// </summary>
    public HealthCheckOptions HealthCheck { get; set; } = new();

    /// <summary>
    /// Gets or sets the circuit breaker options.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

// <copyright file="GatewayRoutingConfig.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Configuration for the API Gateway routing.
/// </summary>
public class GatewayRoutingConfig
{
    /// <summary>
    /// Gets or sets the configuration section name.
    /// </summary>
    public const string SectionName = "GatewayRouting";

    /// <summary>
    /// Gets or sets the routes.
    /// </summary>
    public List<GatewayRoute> Routes { get; set; } = new();

    /// <summary>
    /// Gets or sets the clusters.
    /// </summary>
    public List<GatewayCluster> Clusters { get; set; } = new();

    /// <summary>
    /// Gets or sets the global rate limiting options.
    /// </summary>
    public RateLimitOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// Gets or sets the authentication options.
    /// </summary>
    public AuthOptions Authentication { get; set; } = new();
}

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
    public List<string> Methods { get; set; } = new();

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
    /// Gets or sets the authentication required.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Gets or sets the required scopes.
    /// </summary>
    public List<string> RequiredScopes { get; set; } = new();

    /// <summary>
    /// Gets or sets the request/response transforms.
    /// </summary>
    public List<TransformConfig> Transforms { get; set; } = new();

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
    public List<DestinationConfig> Destinations { get; set; } = new();

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

/// <summary>
/// Represents a destination configuration.
/// </summary>
public class DestinationConfig
{
    /// <summary>
    /// Gets or sets the destination identifier.
    /// </summary>
    public string DestinationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health check address.
    /// </summary>
    public string? HealthCheckAddress { get; set; }

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the weight for weighted load balancing.
    /// </summary>
    public int Weight { get; set; } = 1;
}

/// <summary>
/// Represents rate limiting options.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Gets or sets whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the requests per window.
    /// </summary>
    public int RequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Gets or sets the window size in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the burst capacity.
    /// </summary>
    public int BurstCapacity { get; set; } = 10;
}

/// <summary>
/// Represents authentication options.
/// </summary>
public class AuthOptions
{
    /// <summary>
    /// Gets or sets whether authentication is enabled globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the authentication schemes.
    /// </summary>
    public List<string> Schemes { get; set; } = new() { "Bearer" };

    /// <summary>
    /// Gets or sets the token validation options.
    /// </summary>
    public TokenValidationOptions TokenValidation { get; set; } = new();
}

/// <summary>
/// Represents token validation options.
/// </summary>
public class TokenValidationOptions
{
    /// <summary>
    /// Gets or sets the authority URL.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets the audience.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Gets or sets whether to validate issuer.
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate audience.
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate lifetime.
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;
}

/// <summary>
/// Represents transform configuration.
/// </summary>
public class TransformConfig
{
    /// <summary>
    /// Gets or sets the transform type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transform values.
    /// </summary>
    public Dictionary<string, string> Values { get; set; } = new();
}

/// <summary>
/// Represents health check options.
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Gets or sets whether health checks are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the threshold before marking unhealthy.
    /// </summary>
    public int UnhealthyThreshold { get; set; } = 3;

    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    public string Path { get; set; } = "/health";
}

/// <summary>
/// Represents circuit breaker options.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets whether circuit breaker is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the failure threshold.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration of open state in seconds.
    /// </summary>
    public int DurationOfBreakSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the sampling duration in seconds.
    /// </summary>
    public int SamplingDurationSeconds { get; set; } = 60;
}

/// <summary>
/// Represents retry policy options.
/// </summary>
public class RetryPolicyOptions
{
    /// <summary>
    /// Gets or sets whether retry is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum retry count.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay in milliseconds.
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to use exponential backoff.
    /// </summary>
    public bool ExponentialBackoff { get; set; } = true;
}

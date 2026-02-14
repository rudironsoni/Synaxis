namespace Synaxis.Routing.Health;

/// <summary>
/// Represents the health status of a provider.
/// </summary>
public enum ProviderHealthStatus
{
    /// <summary>
    /// The provider health is unknown (no data).
    /// </summary>
    Unknown,

    /// <summary>
    /// The provider is healthy and functioning normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// The provider has some issues but is still functional.
    /// </summary>
    Warning,

    /// <summary>
    /// The provider is degraded with significant issues.
    /// </summary>
    Degraded,

    /// <summary>
    /// The provider is unhealthy and should not be used.
    /// </summary>
    Unhealthy
}

/// <summary>
/// Represents the result of a health check.
/// </summary>
public class ProviderHealthCheckResult
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public ProviderHealthStatus Status { get; set; }

    /// <summary>
    /// Gets or sets whether the health check passed.
    /// </summary>
    public bool IsHealthy => Status == ProviderHealthStatus.Healthy;

    /// <summary>
    /// Gets or sets the latency of the health check in milliseconds.
    /// </summary>
    public int LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the error message if the check failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional details about the health check.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the health check was performed.
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the success rate percentage (0-100).
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets whether the provider is currently in circuit breaker state.
    /// </summary>
    public bool IsCircuitBreakerOpen { get; set; }
}

/// <summary>
/// Configuration options for health checks.
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Gets or sets the timeout for health checks in milliseconds.
    /// Default is 5000ms.
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the interval between scheduled health checks.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum number of consecutive failures before marking as unhealthy.
    /// Default is 5.
    /// </summary>
    public int MaxConsecutiveFailures { get; set; } = 5;

    /// <summary>
    /// Gets or sets the minimum success rate percentage to be considered healthy.
    /// Default is 90%.
    /// </summary>
    public double MinSuccessRate { get; set; } = 90.0;

    /// <summary>
    /// Gets or sets the maximum average latency in milliseconds to be considered healthy.
    /// Default is 5000ms.
    /// </summary>
    public int MaxAverageLatencyMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the number of health check results to keep in history.
    /// Default is 100.
    /// </summary>
    public int HistorySize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to enable automatic failover on health check failure.
    /// Default is true.
    /// </summary>
    public bool EnableAutoFailover { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable circuit breaker integration.
    /// Default is true.
    /// </summary>
    public bool EnableCircuitBreakerIntegration { get; set; } = true;
}

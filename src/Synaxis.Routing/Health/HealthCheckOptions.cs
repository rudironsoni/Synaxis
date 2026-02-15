namespace Synaxis.Routing.Health;

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

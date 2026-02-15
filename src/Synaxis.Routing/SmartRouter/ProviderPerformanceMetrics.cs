namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Represents performance metrics for a specific provider.
/// </summary>
public class ProviderPerformanceMetrics
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of requests made to this provider.
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of successful requests.
    /// </summary>
    public int SuccessfulRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of failed requests.
    /// </summary>
    public int FailedRequests { get; set; }

    /// <summary>
    /// Gets or sets the success rate as a percentage (0.0 to 100.0).
    /// </summary>
    public double SuccessRate => TotalRequests == 0 ? 0.0 : (double)SuccessfulRequests / TotalRequests * 100.0;

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the P50 latency (median) in milliseconds.
    /// </summary>
    public double P50LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the P95 latency in milliseconds.
    /// </summary>
    public double P95LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the P99 latency in milliseconds.
    /// </summary>
    public double P99LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the total input tokens processed.
    /// </summary>
    public long TotalInputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total output tokens processed.
    /// </summary>
    public long TotalOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total cost incurred.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Gets or sets the average cost per request.
    /// </summary>
    public decimal AverageCostPerRequest => TotalRequests == 0 ? 0 : TotalCost / TotalRequests;

    /// <summary>
    /// Gets or sets the timestamp of the last request.
    /// </summary>
    public DateTime? LastRequestTime { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last successful request.
    /// </summary>
    public DateTime? LastSuccessTime { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last failure.
    /// </summary>
    public DateTime? LastFailureTime { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when metrics were last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

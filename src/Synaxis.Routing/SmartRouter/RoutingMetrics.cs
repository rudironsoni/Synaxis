namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Provides routing metrics for monitoring and analysis.
/// </summary>
public class RoutingMetrics
{
    /// <summary>
    /// Gets or sets the total number of routing decisions made.
    /// </summary>
    public int TotalDecisions { get; set; }

    /// <summary>
    /// Gets or sets the number of successful requests.
    /// </summary>
    public int SuccessfulRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of failed requests.
    /// </summary>
    public int FailedRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of fallback executions.
    /// </summary>
    public int FallbackExecutions { get; set; }

    /// <summary>
    /// Gets or sets the average latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the total cost incurred.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Gets or sets the provider selection counts.
    /// </summary>
    public Dictionary<string, int> ProviderSelectionCounts { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp of the last update.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

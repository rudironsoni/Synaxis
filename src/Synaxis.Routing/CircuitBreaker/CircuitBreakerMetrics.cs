namespace Synaxis.Routing.CircuitBreaker;

/// <summary>
/// Provides metrics about circuit breaker operations.
/// </summary>
public class CircuitBreakerMetrics
{
    /// <summary>
    /// Gets or sets the total number of requests attempted.
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
    /// Gets or sets the number of requests rejected by the circuit breaker.
    /// </summary>
    public int RejectedRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of fallback executions.
    /// </summary>
    public int FallbackExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of times the circuit opened.
    /// </summary>
    public int CircuitOpenedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of times the circuit closed.
    /// </summary>
    public int CircuitClosedCount { get; set; }
}

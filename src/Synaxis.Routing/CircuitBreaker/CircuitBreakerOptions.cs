namespace Synaxis.Routing.CircuitBreaker;

/// <summary>
/// Configuration options for a circuit breaker.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the failure rate threshold (as a percentage) that triggers the circuit to open.
    /// Default is 50%.
    /// </summary>
    public double FailureRateThreshold { get; set; } = 50.0;

    /// <summary>
    /// Gets or sets the minimum number of requests required before the failure rate is calculated.
    /// Default is 10.
    /// </summary>
    public int MinimumRequests { get; set; } = 10;

    /// <summary>
    /// Gets or sets the duration (in milliseconds) that the circuit remains open before transitioning to half-open.
    /// Default is 60000 (1 minute).
    /// </summary>
    public int OpenTimeoutMs { get; set; } = 60000;

    /// <summary>
    /// Gets or sets the number of successful requests required in half-open state to close the circuit.
    /// Default is 3.
    /// </summary>
    public int HalfOpenSuccessThreshold { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay (in milliseconds) for exponential backoff.
    /// Default is 1000 (1 second).
    /// </summary>
    public int ExponentialBackoffBaseMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum delay (in milliseconds) for exponential backoff.
    /// Default is 30000 (30 seconds).
    /// </summary>
    public int ExponentialBackoffMaxMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the backoff multiplier for exponential backoff.
    /// Default is 2.
    /// </summary>
    public double ExponentialBackoffMultiplier { get; set; } = 2.0;
}

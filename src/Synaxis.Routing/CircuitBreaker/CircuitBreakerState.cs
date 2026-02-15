using System;

namespace Synaxis.Routing.CircuitBreaker;

/// <summary>
/// Represents the state of a circuit breaker for serialization.
/// </summary>
public class CircuitBreakerState
{
    public CircuitState State { get; set; }

    public int FailureCount { get; set; }

    public int SuccessCount { get; set; }

    public int TotalRequests { get; set; }

    public DateTime? LastFailureTime { get; set; }

    public DateTime? OpenedAt { get; set; }

    public int ConsecutiveSuccessesInHalfOpen { get; set; }
}

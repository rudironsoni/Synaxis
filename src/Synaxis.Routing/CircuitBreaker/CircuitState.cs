using System;

namespace Synaxis.Routing.CircuitBreaker;

/// <summary>
/// Represents the state of a circuit breaker.
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// The circuit is closed and requests are allowed through.
    /// </summary>
    Closed,

    /// <summary>
    /// The circuit is open and requests are blocked.
    /// </summary>
    Open,

    /// <summary>
    /// The circuit is half-open and a limited number of requests are allowed through to test if the service has recovered.
    /// </summary>
    HalfOpen
}

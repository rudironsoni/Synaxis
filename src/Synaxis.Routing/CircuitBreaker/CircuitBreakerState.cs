// <copyright file="CircuitBreakerState.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing.CircuitBreaker;

using System;

/// <summary>
/// Represents the state of a circuit breaker for serialization.
/// </summary>
public class CircuitBreakerState
{
    /// <summary>
    /// Gets or sets the state of the circuit breaker.
    /// </summary>
    public CircuitState State { get; set; }

    /// <summary>
    /// Gets or sets the number of failures recorded.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the number of successes recorded.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of requests processed.
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the time of the last failure.
    /// </summary>
    public DateTime? LastFailureTime { get; set; }

    /// <summary>
    /// Gets or sets the time when the circuit was opened.
    /// </summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive successes in half-open state.
    /// </summary>
    public int ConsecutiveSuccessesInHalfOpen { get; set; }
}

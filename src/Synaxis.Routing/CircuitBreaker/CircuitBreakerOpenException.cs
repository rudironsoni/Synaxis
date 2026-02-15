using System;

namespace Synaxis.Routing.CircuitBreaker;

/// <summary>
/// Exception thrown when a request is rejected by the circuit breaker.
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Gets the name of the circuit breaker.
    /// </summary>
    public string CircuitBreakerName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    /// <param name="circuitBreakerName">The name of the circuit breaker.</param>
    public CircuitBreakerOpenException(string circuitBreakerName)
        : base($"Circuit breaker '{circuitBreakerName}' is open and rejecting requests.")
    {
        CircuitBreakerName = circuitBreakerName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class with a custom message.
    /// </summary>
    /// <param name="circuitBreakerName">The name of the circuit breaker.</param>
    /// <param name="message">The exception message.</param>
    public CircuitBreakerOpenException(string circuitBreakerName, string message)
        : base(message)
    {
        CircuitBreakerName = circuitBreakerName;
    }
}

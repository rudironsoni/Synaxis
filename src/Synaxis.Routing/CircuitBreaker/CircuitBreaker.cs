// <copyright file="CircuitBreaker.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#pragma warning disable MA0049 // Type name matches namespace - false positive, type has unique name

namespace Synaxis.Routing.CircuitBreaker;

using System;
using System.Threading;

/// <summary>
/// Implements the Circuit Breaker pattern to prevent cascading failures.
/// </summary>
public sealed class CircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly Lock _lock = new();
    private readonly Random _random = new();

    private CircuitState _state = CircuitState.Closed;
    private int _failureCount;
    private int _successCount;
    private int _totalRequests;
    private DateTime? _lastFailureTime;
    private DateTime? _openedAt;
    private int _consecutiveSuccessesInHalfOpen;

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (this._lock)
            {
                return this._state;
            }
        }
    }

    /// <summary>
    /// Gets the number of failures recorded.
    /// </summary>
    public int FailureCount
    {
        get
        {
            lock (this._lock)
            {
                return this._failureCount;
            }
        }
    }

    /// <summary>
    /// Gets the number of successes recorded.
    /// </summary>
    public int SuccessCount
    {
        get
        {
            lock (this._lock)
            {
                return this._successCount;
            }
        }
    }

    /// <summary>
    /// Gets the total number of requests processed.
    /// </summary>
    public int TotalRequests
    {
        get
        {
            lock (this._lock)
            {
                return this._totalRequests;
            }
        }
    }

    /// <summary>
    /// Gets the current failure rate as a percentage.
    /// </summary>
    public double FailureRate
    {
        get
        {
            lock (this._lock)
            {
                return this._totalRequests == 0 ? 0.0 : (double)this._failureCount / this._totalRequests * 100.0;
            }
        }
    }

    /// <summary>
    /// Gets the timestamp of the last failure, or null if no failures have occurred.
    /// </summary>
    public DateTime? LastFailureTime
    {
        get
        {
            lock (this._lock)
            {
                return this._lastFailureTime;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
    /// </summary>
    /// <param name="name">The name of the circuit breaker.</param>
    /// <param name="options">The configuration options.</param>
    public CircuitBreaker(string name, CircuitBreakerOptions options)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);
        this._options = options;
    }

    /// <summary>
    /// Records a successful request.
    /// </summary>
    public void RecordSuccess()
    {
        lock (this._lock)
        {
            this._totalRequests++;
            this._successCount++;
            this._lastFailureTime = null;

            switch (this._state)
            {
                case CircuitState.Closed:
                    // In closed state, just track metrics
                    break;

                case CircuitState.HalfOpen:
                    this._consecutiveSuccessesInHalfOpen++;
                    if (this._consecutiveSuccessesInHalfOpen >= this._options.HalfOpenSuccessThreshold)
                    {
                        this.TransitionToClosed();
                    }

                    break;

                case CircuitState.Open:
                    // Should not happen, but handle gracefully
                    break;
            }
        }
    }

    /// <summary>
    /// Records a failed request.
    /// </summary>
    public void RecordFailure()
    {
        lock (this._lock)
        {
            this._totalRequests++;
            this._failureCount++;
            this._lastFailureTime = DateTime.UtcNow;

            switch (this._state)
            {
                case CircuitState.Closed:
                    if (this._totalRequests >= this._options.MinimumRequests)
                    {
                        double failureRate = (double)this._failureCount / this._totalRequests * 100.0;
                        if (failureRate >= this._options.FailureRateThreshold)
                        {
                            this.TransitionToOpen();
                        }
                    }

                    break;

                case CircuitState.HalfOpen:
                    // Any failure in half-open state immediately opens the circuit
                    this.TransitionToOpen();
                    break;

                case CircuitState.Open:
                    // Already open, just track metrics
                    break;
            }
        }
    }

    /// <summary>
    /// Determines whether a request should be allowed through the circuit.
    /// </summary>
    /// <returns><c>true</c> if the request should be allowed; otherwise, <c>false</c>.</returns>
    public bool AllowRequest()
    {
        lock (this._lock)
        {
            switch (this._state)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    // Check if we should transition to half-open
                    if (this._openedAt.HasValue && (DateTime.UtcNow - this._openedAt.Value).TotalMilliseconds >= this._options.OpenTimeoutMs)
                    {
                        this.TransitionToHalfOpen();
                        return true;
                    }

                    return false;

                case CircuitState.HalfOpen:
                    return true;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Calculates the exponential backoff delay for the next retry attempt.
    /// </summary>
    /// <param name="attemptNumber">The attempt number (1-based).</param>
    /// <returns>The delay in milliseconds.</returns>
    public int CalculateBackoffDelay(int attemptNumber)
    {
        if (attemptNumber < 1)
        {
            attemptNumber = 1;
        }

        double delay = this._options.ExponentialBackoffBaseMs * Math.Pow(this._options.ExponentialBackoffMultiplier, attemptNumber - 1);
        delay = Math.Min(delay, this._options.ExponentialBackoffMaxMs);

        // Add jitter to prevent thundering herd
        double jitter = delay * 0.1 * ((this._random.NextDouble() * 2) - 1);
        delay += jitter;

        return (int)Math.Max(0, delay);
    }

    /// <summary>
    /// Resets the circuit breaker to its initial closed state.
    /// </summary>
    public void Reset()
    {
        lock (this._lock)
        {
            this._state = CircuitState.Closed;
            this._failureCount = 0;
            this._successCount = 0;
            this._totalRequests = 0;
            this._lastFailureTime = null;
            this._openedAt = null;
            this._consecutiveSuccessesInHalfOpen = 0;
        }
    }

    private void TransitionToOpen()
    {
        this._state = CircuitState.Open;
        this._openedAt = DateTime.UtcNow;
        this._consecutiveSuccessesInHalfOpen = 0;
    }

    private void TransitionToHalfOpen()
    {
        this._state = CircuitState.HalfOpen;
        this._openedAt = null;
        this._consecutiveSuccessesInHalfOpen = 0;
    }

    private void TransitionToClosed()
    {
        this._state = CircuitState.Closed;
        this._failureCount = 0;
        this._successCount = 0;
        this._totalRequests = 0;
        this._lastFailureTime = null;
        this._openedAt = null;
        this._consecutiveSuccessesInHalfOpen = 0;
    }
}

#pragma warning restore MA0049

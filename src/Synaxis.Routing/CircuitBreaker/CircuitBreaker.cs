using System;
using System.Threading;

namespace Synaxis.Routing.CircuitBreaker;

/// <summary>
/// Implements the Circuit Breaker pattern to prevent cascading failures.
/// </summary>
public class CircuitBreaker
{
    private readonly string _name;
    private readonly CircuitBreakerOptions _options;
    private readonly object _lock = new();
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
            lock (_lock)
            {
                return _state;
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
            lock (_lock)
            {
                return _failureCount;
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
            lock (_lock)
            {
                return _successCount;
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
            lock (_lock)
            {
                return _totalRequests;
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
            lock (_lock)
            {
                return _totalRequests == 0 ? 0.0 : (double)_failureCount / _totalRequests * 100.0;
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
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Records a successful request.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _totalRequests++;
            _successCount++;
            _lastFailureTime = null;

            switch (_state)
            {
                case CircuitState.Closed:
                    // In closed state, just track metrics
                    break;

                case CircuitState.HalfOpen:
                    _consecutiveSuccessesInHalfOpen++;
                    if (_consecutiveSuccessesInHalfOpen >= _options.HalfOpenSuccessThreshold)
                    {
                        TransitionToClosed();
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
        lock (_lock)
        {
            _totalRequests++;
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            switch (_state)
            {
                case CircuitState.Closed:
                    if (_totalRequests >= _options.MinimumRequests)
                    {
                        double failureRate = (double)_failureCount / _totalRequests * 100.0;
                        if (failureRate >= _options.FailureRateThreshold)
                        {
                            TransitionToOpen();
                        }
                    }
                    break;

                case CircuitState.HalfOpen:
                    // Any failure in half-open state immediately opens the circuit
                    TransitionToOpen();
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
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    // Check if we should transition to half-open
                    if (_openedAt.HasValue && (DateTime.UtcNow - _openedAt.Value).TotalMilliseconds >= _options.OpenTimeoutMs)
                    {
                        TransitionToHalfOpen();
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

        double delay = _options.ExponentialBackoffBaseMs * Math.Pow(_options.ExponentialBackoffMultiplier, attemptNumber - 1);
        delay = Math.Min(delay, _options.ExponentialBackoffMaxMs);

        // Add jitter to prevent thundering herd
        double jitter = delay * 0.1 * (_random.NextDouble() * 2 - 1);
        delay += jitter;

        return (int)Math.Max(0, delay);
    }

    /// <summary>
    /// Resets the circuit breaker to its initial closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitState.Closed;
            _failureCount = 0;
            _successCount = 0;
            _totalRequests = 0;
            _lastFailureTime = null;
            _openedAt = null;
            _consecutiveSuccessesInHalfOpen = 0;
        }
    }

    private void TransitionToOpen()
    {
        _state = CircuitState.Open;
        _openedAt = DateTime.UtcNow;
        _consecutiveSuccessesInHalfOpen = 0;
    }

    private void TransitionToHalfOpen()
    {
        _state = CircuitState.HalfOpen;
        _openedAt = null;
        _consecutiveSuccessesInHalfOpen = 0;
    }

    private void TransitionToClosed()
    {
        _state = CircuitState.Closed;
        _failureCount = 0;
        _successCount = 0;
        _totalRequests = 0;
        _lastFailureTime = null;
        _openedAt = null;
        _consecutiveSuccessesInHalfOpen = 0;
    }
}

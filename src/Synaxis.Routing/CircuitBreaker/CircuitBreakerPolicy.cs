using System;
using System.Threading;
using System.Threading.Tasks;

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

/// <summary>
/// A generic policy wrapper for executing operations through a circuit breaker.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
public class CircuitBreakerPolicy<TResult>
{
    private readonly CircuitBreaker _circuitBreaker;
    private readonly CircuitBreakerMetrics _metrics;
    private readonly Func<Exception, bool> _exceptionPredicate;
    private readonly Func<CancellationToken, Task<TResult>>? _fallback;

    /// <summary>
    /// Gets the circuit breaker associated with this policy.
    /// </summary>
    public CircuitBreaker CircuitBreaker => _circuitBreaker;

    /// <summary>
    /// Gets the metrics for this policy.
    /// </summary>
    public CircuitBreakerMetrics Metrics => _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerPolicy{TResult}"/> class.
    /// </summary>
    /// <param name="circuitBreaker">The circuit breaker to use.</param>
    /// <param name="exceptionPredicate">A predicate to determine which exceptions should be treated as failures.</param>
    /// <param name="fallback">An optional fallback function to execute when the circuit is open or an exception occurs.</param>
    public CircuitBreakerPolicy(
        CircuitBreaker circuitBreaker,
        Func<Exception, bool>? exceptionPredicate = null,
        Func<CancellationToken, Task<TResult>>? fallback = null)
    {
        _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
        _exceptionPredicate = exceptionPredicate ?? (ex => true);
        _fallback = fallback;
        _metrics = new CircuitBreakerMetrics();
    }

    /// <summary>
    /// Executes the specified operation through the circuit breaker.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit breaker is open and no fallback is provided.</exception>
    public async Task<TResult> ExecuteAsync(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        _metrics.TotalRequests++;

        // Check if the circuit allows the request
        if (!_circuitBreaker.AllowRequest())
        {
            _metrics.RejectedRequests++;

            if (_fallback != null)
            {
                _metrics.FallbackExecutions++;
                return await _fallback(cancellationToken).ConfigureAwait(false);
            }

            throw new CircuitBreakerOpenException(_circuitBreaker.GetType().Name);
        }

        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            _circuitBreaker.RecordSuccess();
            _metrics.SuccessfulRequests++;
            return result;
        }
        catch (Exception ex) when (_exceptionPredicate(ex))
        {
            _circuitBreaker.RecordFailure();
            _metrics.FailedRequests++;

            if (_fallback != null)
            {
                _metrics.FallbackExecutions++;
                return await _fallback(cancellationToken).ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <summary>
    /// Executes the specified operation through the circuit breaker with retry logic.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<TResult> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<TResult>> operation,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
            }
            catch (CircuitBreakerOpenException)
            {
                // Don't retry on circuit open exceptions
                throw;
            }
            catch (Exception ex) when (_exceptionPredicate(ex))
            {
                lastException = ex;

                if (attempt < maxRetries)
                {
                    int delay = _circuitBreaker.CalculateBackoffDelay(attempt);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        if (_fallback != null)
        {
            _metrics.FallbackExecutions++;
            return await _fallback(cancellationToken).ConfigureAwait(false);
        }

        throw lastException ?? new InvalidOperationException("Operation failed after retries.");
    }

    /// <summary>
    /// Resets the circuit breaker and metrics.
    /// </summary>
    public void Reset()
    {
        _circuitBreaker.Reset();
        _metrics.TotalRequests = 0;
        _metrics.SuccessfulRequests = 0;
        _metrics.FailedRequests = 0;
        _metrics.RejectedRequests = 0;
        _metrics.FallbackExecutions = 0;
        _metrics.CircuitOpenedCount = 0;
        _metrics.CircuitClosedCount = 0;
    }
}

/// <summary>
/// A non-generic policy wrapper for executing operations through a circuit breaker.
/// </summary>
public class CircuitBreakerPolicy
{
    private readonly CircuitBreaker _circuitBreaker;
    private readonly CircuitBreakerMetrics _metrics;
    private readonly Func<Exception, bool> _exceptionPredicate;
    private readonly Func<CancellationToken, Task>? _fallback;

    /// <summary>
    /// Gets the circuit breaker associated with this policy.
    /// </summary>
    public CircuitBreaker CircuitBreaker => _circuitBreaker;

    /// <summary>
    /// Gets the metrics for this policy.
    /// </summary>
    public CircuitBreakerMetrics Metrics => _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerPolicy"/> class.
    /// </summary>
    /// <param name="circuitBreaker">The circuit breaker to use.</param>
    /// <param name="exceptionPredicate">A predicate to determine which exceptions should be treated as failures.</param>
    /// <param name="fallback">An optional fallback function to execute when the circuit is open or an exception occurs.</param>
    public CircuitBreakerPolicy(
        CircuitBreaker circuitBreaker,
        Func<Exception, bool>? exceptionPredicate = null,
        Func<CancellationToken, Task>? fallback = null)
    {
        _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
        _exceptionPredicate = exceptionPredicate ?? (ex => true);
        _fallback = fallback;
        _metrics = new CircuitBreakerMetrics();
    }

    /// <summary>
    /// Executes the specified operation through the circuit breaker.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit breaker is open and no fallback is provided.</exception>
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        _metrics.TotalRequests++;

        // Check if the circuit allows the request
        if (!_circuitBreaker.AllowRequest())
        {
            _metrics.RejectedRequests++;

            if (_fallback != null)
            {
                _metrics.FallbackExecutions++;
                await _fallback(cancellationToken).ConfigureAwait(false);
                return;
            }

            throw new CircuitBreakerOpenException(_circuitBreaker.GetType().Name);
        }

        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
            _circuitBreaker.RecordSuccess();
            _metrics.SuccessfulRequests++;
        }
        catch (Exception ex) when (_exceptionPredicate(ex))
        {
            _circuitBreaker.RecordFailure();
            _metrics.FailedRequests++;

            if (_fallback != null)
            {
                _metrics.FallbackExecutions++;
                await _fallback(cancellationToken).ConfigureAwait(false);
                return;
            }

            throw;
        }
    }

    /// <summary>
    /// Executes the specified operation through the circuit breaker with retry logic.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    public async Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> operation,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (CircuitBreakerOpenException)
            {
                // Don't retry on circuit open exceptions
                throw;
            }
            catch (Exception ex) when (_exceptionPredicate(ex))
            {
                lastException = ex;

                if (attempt < maxRetries)
                {
                    int delay = _circuitBreaker.CalculateBackoffDelay(attempt);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        if (_fallback != null)
        {
            _metrics.FallbackExecutions++;
            await _fallback(cancellationToken).ConfigureAwait(false);
            return;
        }

        throw lastException ?? new InvalidOperationException("Operation failed after retries.");
    }

    /// <summary>
    /// Creates a generic circuit breaker policy for operations that return a result.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <param name="fallback">An optional fallback function to execute when the circuit is open or an exception occurs.</param>
    /// <returns>A new generic circuit breaker policy.</returns>
    public CircuitBreakerPolicy<TResult> AsGenericPolicy<TResult>(Func<CancellationToken, Task<TResult>>? fallback = null)
    {
        return new CircuitBreakerPolicy<TResult>(_circuitBreaker, _exceptionPredicate, fallback);
    }

    /// <summary>
    /// Resets the circuit breaker and metrics.
    /// </summary>
    public void Reset()
    {
        _circuitBreaker.Reset();
        _metrics.TotalRequests = 0;
        _metrics.SuccessfulRequests = 0;
        _metrics.FailedRequests = 0;
        _metrics.RejectedRequests = 0;
        _metrics.FallbackExecutions = 0;
        _metrics.CircuitOpenedCount = 0;
        _metrics.CircuitClosedCount = 0;
    }
}

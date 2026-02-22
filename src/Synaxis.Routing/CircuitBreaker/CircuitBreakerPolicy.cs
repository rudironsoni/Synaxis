// <copyright file="CircuitBreakerPolicy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#pragma warning disable SA1402 // File contains multiple types - generic and non-generic versions are intentionally in the same file
namespace Synaxis.Routing.CircuitBreaker;

using System;
using System.Threading;
using System.Threading.Tasks;

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
    public CircuitBreaker CircuitBreaker => this._circuitBreaker;

    /// <summary>
    /// Gets the metrics for this policy.
    /// </summary>
    public CircuitBreakerMetrics Metrics => this._metrics;

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
        ArgumentNullException.ThrowIfNull(circuitBreaker);
        this._circuitBreaker = circuitBreaker;
        this._exceptionPredicate = exceptionPredicate ?? (ex => true);
        this._fallback = fallback;
        this._metrics = new CircuitBreakerMetrics();
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
        ArgumentNullException.ThrowIfNull(operation);
        this._metrics.TotalRequests++;

        // Check if the circuit allows the request
        if (!this._circuitBreaker.AllowRequest())
        {
            this._metrics.RejectedRequests++;

            if (this._fallback != null)
            {
                this._metrics.FallbackExecutions++;
                return await this._fallback(cancellationToken).ConfigureAwait(false);
            }

            throw new CircuitBreakerOpenException(this._circuitBreaker.GetType().Name);
        }

        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            this._circuitBreaker.RecordSuccess();
            this._metrics.SuccessfulRequests++;
            return result;
        }
        catch (Exception ex) when (this._exceptionPredicate(ex))
        {
            this._circuitBreaker.RecordFailure();
            this._metrics.FailedRequests++;

            if (this._fallback != null)
            {
                this._metrics.FallbackExecutions++;
                return await this._fallback(cancellationToken).ConfigureAwait(false);
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
        ArgumentNullException.ThrowIfNull(operation);
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await this.ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
            }
            catch (CircuitBreakerOpenException)
            {
                // Don't retry on circuit open exceptions
                throw;
            }
            catch (Exception ex) when (this._exceptionPredicate(ex))
            {
                lastException = ex;

                if (attempt < maxRetries)
                {
                    int delay = this._circuitBreaker.CalculateBackoffDelay(attempt);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        if (this._fallback != null)
        {
            this._metrics.FallbackExecutions++;
            return await this._fallback(cancellationToken).ConfigureAwait(false);
        }

        throw lastException ?? new InvalidOperationException("Operation failed after retries.");
    }

    /// <summary>
    /// Resets the circuit breaker and metrics.
    /// </summary>
    public void Reset()
    {
        this._circuitBreaker.Reset();
        this._metrics.TotalRequests = 0;
        this._metrics.SuccessfulRequests = 0;
        this._metrics.FailedRequests = 0;
        this._metrics.RejectedRequests = 0;
        this._metrics.FallbackExecutions = 0;
        this._metrics.CircuitOpenedCount = 0;
        this._metrics.CircuitClosedCount = 0;
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
    public CircuitBreaker CircuitBreaker => this._circuitBreaker;

    /// <summary>
    /// Gets the metrics for this policy.
    /// </summary>
    public CircuitBreakerMetrics Metrics => this._metrics;

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
        ArgumentNullException.ThrowIfNull(circuitBreaker);
        this._circuitBreaker = circuitBreaker;
        this._exceptionPredicate = exceptionPredicate ?? (ex => true);
        this._fallback = fallback;
        this._metrics = new CircuitBreakerMetrics();
    }

    /// <summary>
    /// Executes the specified operation through the circuit breaker.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit breaker is open and no fallback is provided.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        this._metrics.TotalRequests++;

        // Check if the circuit allows the request
        if (!this._circuitBreaker.AllowRequest())
        {
            this._metrics.RejectedRequests++;

            if (this._fallback != null)
            {
                this._metrics.FallbackExecutions++;
                await this._fallback(cancellationToken).ConfigureAwait(false);
                return;
            }

            throw new CircuitBreakerOpenException(this._circuitBreaker.GetType().Name);
        }

        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
            this._circuitBreaker.RecordSuccess();
            this._metrics.SuccessfulRequests++;
        }
        catch (Exception ex) when (this._exceptionPredicate(ex))
        {
            this._circuitBreaker.RecordFailure();
            this._metrics.FailedRequests++;

            if (this._fallback != null)
            {
                this._metrics.FallbackExecutions++;
                await this._fallback(cancellationToken).ConfigureAwait(false);
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
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> operation,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await this.ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (CircuitBreakerOpenException)
            {
                // Don't retry on circuit open exceptions
                throw;
            }
            catch (Exception ex) when (this._exceptionPredicate(ex))
            {
                lastException = ex;

                if (attempt < maxRetries)
                {
                    int delay = this._circuitBreaker.CalculateBackoffDelay(attempt);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        if (this._fallback != null)
        {
            this._metrics.FallbackExecutions++;
            await this._fallback(cancellationToken).ConfigureAwait(false);
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
        return new CircuitBreakerPolicy<TResult>(this._circuitBreaker, this._exceptionPredicate, fallback);
    }

    /// <summary>
    /// Resets the circuit breaker and metrics.
    /// </summary>
    public void Reset()
    {
        this._circuitBreaker.Reset();
        this._metrics.TotalRequests = 0;
        this._metrics.SuccessfulRequests = 0;
        this._metrics.FailedRequests = 0;
        this._metrics.RejectedRequests = 0;
        this._metrics.FallbackExecutions = 0;
        this._metrics.CircuitOpenedCount = 0;
        this._metrics.CircuitClosedCount = 0;
    }
}

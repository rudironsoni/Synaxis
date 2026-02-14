using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Synaxis.Routing.CircuitBreaker;
using Synaxis.Routing.SmartRouter;
using CircuitBreakerImpl = Synaxis.Routing.CircuitBreaker.CircuitBreaker;

namespace Synaxis.Routing.Health;

/// <summary>
/// Event arguments for health status changes.
/// </summary>
public class HealthStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous health status.
    /// </summary>
    public ProviderHealthStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new health status.
    /// </summary>
    public ProviderHealthStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the health check result.
    /// </summary>
    public ProviderHealthCheckResult? CheckResult { get; set; }
}

/// <summary>
/// Health check service for monitoring AI provider health.
/// </summary>
public class ProviderHealthMonitor : IProviderHealthChecker
{
    private readonly HealthCheckOptions _options;
    private readonly ProviderPerformanceTracker _performanceTracker;
    private readonly ConcurrentDictionary<string, CircuitBreakerImpl?> _circuitBreakers;
    private readonly ConcurrentDictionary<string, List<ProviderHealthCheckResult>> _healthHistory;
    private readonly ConcurrentDictionary<string, ProviderHealthStatus> _currentStatus;
    private readonly ConcurrentDictionary<string, int> _consecutiveFailures;
    private readonly ILogger<ProviderHealthMonitor>? _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Event raised when a provider's health status changes.
    /// </summary>
    public event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderHealthMonitor"/> class.
    /// </summary>
    /// <param name="options">The health check options.</param>
    /// <param name="performanceTracker">The performance tracker.</param>
    /// <param name="logger">The logger.</param>
    public ProviderHealthMonitor(
        HealthCheckOptions? options = null,
        ProviderPerformanceTracker? performanceTracker = null,
        ILogger<ProviderHealthMonitor>? logger = null)
    {
        _options = options ?? new HealthCheckOptions();
        _performanceTracker = performanceTracker ?? new ProviderPerformanceTracker();
        _circuitBreakers = new ConcurrentDictionary<string, CircuitBreakerImpl?>();
        _healthHistory = new ConcurrentDictionary<string, List<ProviderHealthCheckResult>>();
        _currentStatus = new ConcurrentDictionary<string, ProviderHealthStatus>();
        _consecutiveFailures = new ConcurrentDictionary<string, int>();
        _logger = logger;
    }

    /// <summary>
    /// Registers a circuit breaker for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="circuitBreaker">The circuit breaker.</param>
    public void RegisterCircuitBreaker(string providerId, CircuitBreakerImpl? circuitBreaker)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));
        }

        _circuitBreakers.AddOrUpdate(providerId, circuitBreaker, (_, _) => circuitBreaker);
        _logger?.LogInformation("Registered circuit breaker for provider {ProviderId}", providerId);
    }

    /// <summary>
    /// Performs a health check on a provider.
    /// </summary>
    /// <param name="providerId">The provider ID to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The health check result.</returns>
    public async Task<ProviderHealthCheckResult> CheckHealthAsync(string providerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new ProviderHealthCheckResult
        {
            ProviderId = providerId,
            CheckTime = DateTime.UtcNow
        };

        try
        {
            await PerformHealthCheckAsync(providerId, result, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            HandleTimeout(result, stopwatch, ex);
        }
        catch (Exception ex)
        {
            HandleException(result, stopwatch, ex, providerId);
        }

        return result;
    }

    /// <summary>
    /// Performs health checks on multiple providers concurrently.
    /// </summary>
    /// <param name="providerIds">The provider IDs to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A dictionary of provider IDs to their health check results.</returns>
    public async Task<Dictionary<string, ProviderHealthCheckResult>> CheckHealthAsync(
        IEnumerable<string> providerIds,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, ProviderHealthCheckResult>();
        var tasks = providerIds.Select(async providerId =>
        {
            var result = await CheckHealthAsync(providerId, cancellationToken);
            return (providerId, result);
        });

        var completedTasks = await Task.WhenAll(tasks);
        foreach (var (providerId, result) in completedTasks)
        {
            results[providerId] = result;
        }

        return results;
    }

    private async Task PerformHealthCheckAsync(
        string providerId,
        ProviderHealthCheckResult result,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Use a timeout for the health check
        using var timeoutCts = new CancellationTokenSource(_options.TimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Get performance metrics
        var metrics = _performanceTracker.GetMetrics(providerId);

        if (metrics == null)
        {
            result.Status = ProviderHealthStatus.Unknown;
            result.ErrorMessage = "No performance metrics available";
            result.SuccessRate = 0;
            result.AverageLatencyMs = 0;
        }
        else
        {
            PopulateResultFromMetrics(result, metrics, providerId);
        }

        stopwatch.Stop();
        result.LatencyMs = (int)stopwatch.ElapsedMilliseconds;

        // Update health history
        UpdateHealthHistory(providerId, result);

        // Update current status and check for changes
        UpdateHealthStatus(providerId, result);

        _logger?.LogInformation(
            "Health check for provider {ProviderId}: Status={Status}, Latency={Latency}ms, SuccessRate={SuccessRate:F2}%",
            providerId,
            result.Status,
            result.LatencyMs,
            result.SuccessRate);
    }

    private void PopulateResultFromMetrics(
        ProviderHealthCheckResult result,
        ProviderPerformanceMetrics metrics,
        string providerId)
    {
        // Calculate health status based on metrics
        result.SuccessRate = metrics.SuccessRate;
        result.AverageLatencyMs = metrics.AverageLatencyMs;
        result.ConsecutiveFailures = metrics.ConsecutiveFailures;

        // Check circuit breaker state
        if (_options.EnableCircuitBreakerIntegration &&
            _circuitBreakers.TryGetValue(providerId, out var circuitBreaker) &&
            circuitBreaker != null)
        {
            result.IsCircuitBreakerOpen = !circuitBreaker.AllowRequest();
        }

        // Determine health status
        result.Status = DetermineHealthStatus(metrics, result.IsCircuitBreakerOpen);

        // Add details
        result.Details["total_requests"] = metrics.TotalRequests;
        result.Details["successful_requests"] = metrics.SuccessfulRequests;
        result.Details["failed_requests"] = metrics.FailedRequests;
        result.Details["p50_latency_ms"] = metrics.P50LatencyMs;
        result.Details["p95_latency_ms"] = metrics.P95LatencyMs;
        result.Details["p99_latency_ms"] = metrics.P99LatencyMs;
        result.Details["last_success_time"] = metrics.LastSuccessTime;
        result.Details["last_failure_time"] = metrics.LastFailureTime;
    }

    private void HandleTimeout(
        ProviderHealthCheckResult result,
        System.Diagnostics.Stopwatch stopwatch,
        OperationCanceledException ex)
    {
        stopwatch.Stop();
        result.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
        result.Status = ProviderHealthStatus.Unhealthy;
        result.ErrorMessage = "Health check timed out";
        result.SuccessRate = 0;
        result.AverageLatencyMs = 0;

        _logger?.LogWarning(ex, "Health check for provider {ProviderId} timed out", result.ProviderId);
    }

    private void HandleException(
        ProviderHealthCheckResult result,
        System.Diagnostics.Stopwatch stopwatch,
        Exception ex,
        string providerId)
    {
        stopwatch.Stop();
        result.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
        result.Status = ProviderHealthStatus.Unhealthy;
        result.ErrorMessage = ex.Message;
        result.SuccessRate = 0;
        result.AverageLatencyMs = 0;

        _logger?.LogError(ex, "Health check for provider {ProviderId} failed", providerId);
    }

    /// <summary>
    /// Gets the current health status of a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The current health status.</returns>
    public Task<ProviderHealthStatus> GetHealthStatusAsync(string providerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));
        }

        var status = _currentStatus.TryGetValue(providerId, out var s) ? s : ProviderHealthStatus.Unknown;
        return Task.FromResult(status);
    }

    /// <summary>
    /// Gets the health history for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A list of health check results.</returns>
    public Task<List<ProviderHealthCheckResult>> GetHealthHistoryAsync(
        string providerId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));
        }

        var history = _healthHistory.TryGetValue(providerId, out var h)
            ? h.TakeLast(limit).ToList()
            : new List<ProviderHealthCheckResult>();

        return Task.FromResult(history);
    }

    /// <summary>
    /// Gets all current health statuses.
    /// </summary>
    /// <returns>A dictionary of provider IDs to their health statuses.</returns>
    public Dictionary<string, ProviderHealthStatus> GetAllHealthStatuses()
    {
        return _currentStatus.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Resets the health history for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    public void ResetHealthHistory(string providerId)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            return;
        }

        _healthHistory.TryRemove(providerId, out _);
        _currentStatus.TryRemove(providerId, out _);
        _consecutiveFailures.TryRemove(providerId, out _);

        _logger?.LogInformation("Reset health history for provider {ProviderId}", providerId);
    }

    /// <summary>
    /// Resets all health history.
    /// </summary>
    public void ResetAll()
    {
        _healthHistory.Clear();
        _currentStatus.Clear();
        _consecutiveFailures.Clear();

        _logger?.LogInformation("Reset all health history");
    }

    private ProviderHealthStatus DetermineHealthStatus(
        ProviderPerformanceMetrics metrics,
        bool isCircuitBreakerOpen)
    {
        // If circuit breaker is open, provider is unhealthy
        if (isCircuitBreakerOpen)
        {
            return ProviderHealthStatus.Unhealthy;
        }

        // If no requests yet, status is unknown
        if (metrics.TotalRequests == 0)
        {
            return ProviderHealthStatus.Unknown;
        }

        // Check consecutive failures
        if (metrics.ConsecutiveFailures >= _options.MaxConsecutiveFailures)
        {
            return ProviderHealthStatus.Unhealthy;
        }

        // Check success rate
        if (metrics.SuccessRate < 50.0)
        {
            return ProviderHealthStatus.Degraded;
        }

        if (metrics.SuccessRate < _options.MinSuccessRate)
        {
            return ProviderHealthStatus.Warning;
        }

        // Check latency
        if (metrics.AverageLatencyMs > _options.MaxAverageLatencyMs * 2)
        {
            return ProviderHealthStatus.Degraded;
        }

        if (metrics.AverageLatencyMs > _options.MaxAverageLatencyMs)
        {
            return ProviderHealthStatus.Warning;
        }

        return ProviderHealthStatus.Healthy;
    }

    private void UpdateHealthHistory(string providerId, ProviderHealthCheckResult result)
    {
        var history = _healthHistory.GetOrAdd(providerId, _ => new List<ProviderHealthCheckResult>());

        lock (_lock)
        {
            history.Add(result);
            if (history.Count > _options.HistorySize)
            {
                history.RemoveAt(0);
            }
        }
    }

    private void UpdateHealthStatus(string providerId, ProviderHealthCheckResult result)
    {
        var previousStatus = _currentStatus.TryGetValue(providerId, out var status) ? status : ProviderHealthStatus.Unknown;
        var newStatus = result.Status;

        // Update consecutive failures
        if (result.Status == ProviderHealthStatus.Unhealthy)
        {
            _consecutiveFailures.AddOrUpdate(providerId, 1, (_, current) => current + 1);
        }
        else
        {
            _consecutiveFailures.AddOrUpdate(providerId, 0, (_, _) => 0);
        }

        // Update current status
        _currentStatus.AddOrUpdate(providerId, newStatus, (_, _) => newStatus);

        // Raise event if status changed
        if (previousStatus != newStatus)
        {
            var eventArgs = new HealthStatusChangedEventArgs
            {
                ProviderId = providerId,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                CheckResult = result
            };

            HealthStatusChanged?.Invoke(this, eventArgs);

            _logger?.LogWarning(
                "Provider {ProviderId} health status changed from {PreviousStatus} to {NewStatus}",
                providerId,
                previousStatus,
                newStatus);
        }
    }
}

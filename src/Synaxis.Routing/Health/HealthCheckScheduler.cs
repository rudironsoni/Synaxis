using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Synaxis.Routing.SmartRouter;

namespace Synaxis.Routing.Health;

/// <summary>
/// Event arguments for health check completion.
/// </summary>
public class HealthCheckCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the health check result.
    /// </summary>
    public ProviderHealthCheckResult Result { get; set; } = new();
}

/// <summary>
/// Scheduled health check service for automated provider health monitoring.
/// </summary>
public sealed class HealthCheckScheduler : IDisposable
{
    private readonly HealthCheckOptions _options;
    private readonly IProviderHealthChecker _healthChecker;
    private readonly ConcurrentDictionary<string, Provider> _providers;
    private readonly ConcurrentDictionary<string, DateTime> _lastCheckTimes;
    private readonly ILogger<HealthCheckScheduler>? _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _schedulerTask;
    private bool _disposed;

    /// <summary>
    /// Event raised when a health check completes.
    /// </summary>
    public event EventHandler<HealthCheckCompletedEventArgs>? HealthCheckCompleted;

    /// <summary>
    /// Event raised when a provider's health status changes.
    /// </summary>
    public event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

    /// <summary>
    /// Gets whether the scheduler is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the number of providers being monitored.
    /// </summary>
    public int MonitoredProviderCount => _providers.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckScheduler"/> class.
    /// </summary>
    /// <param name="healthChecker">The health checker to use.</param>
    /// <param name="options">The health check options.</param>
    /// <param name="logger">The logger.</param>
    public HealthCheckScheduler(
        IProviderHealthChecker healthChecker,
        HealthCheckOptions? options = null,
        ILogger<HealthCheckScheduler>? logger = null)
    {
        _healthChecker = healthChecker ?? throw new ArgumentNullException(nameof(healthChecker));
        _options = options ?? new HealthCheckOptions();
        _providers = new ConcurrentDictionary<string, Provider>();
        _lastCheckTimes = new ConcurrentDictionary<string, DateTime>();
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();

        // Subscribe to health status changes
        if (_healthChecker is ProviderHealthMonitor monitor)
        {
            monitor.HealthStatusChanged += (sender, args) =>
            {
                HealthStatusChanged?.Invoke(this, args);
            };
        }
    }

    /// <summary>
    /// Starts the health check scheduler.
    /// </summary>
    public void Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HealthCheckScheduler));
        }

        if (IsRunning)
        {
            _logger?.LogWarning("Health check scheduler is already running");
            return;
        }

        IsRunning = true;
        _schedulerTask = RunSchedulerAsync(_cancellationTokenSource.Token);

        _logger?.LogInformation("Health check scheduler started with interval {Interval}", _options.CheckInterval);
    }

    /// <summary>
    /// Stops the health check scheduler.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        await _cancellationTokenSource.CancelAsync();

        if (_schedulerTask != null)
        {
            await _schedulerTask;
        }

        _logger?.LogInformation("Health check scheduler stopped");
    }

    /// <summary>
    /// Adds a provider to monitor.
    /// </summary>
    /// <param name="provider">The provider to monitor.</param>
    public void AddProvider(Provider provider)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (string.IsNullOrEmpty(provider.Id))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(provider));
        }

        _providers.AddOrUpdate(provider.Id, provider, (_, _) => provider);
        _logger?.LogInformation("Added provider {ProviderId} to health monitoring", provider.Id);
    }

    /// <summary>
    /// Removes a provider from monitoring.
    /// </summary>
    /// <param name="providerId">The provider ID to remove.</param>
    public void RemoveProvider(string providerId)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));
        }

        _providers.TryRemove(providerId, out _);
        _lastCheckTimes.TryRemove(providerId, out _);

        _logger?.LogInformation("Removed provider {ProviderId} from health monitoring", providerId);
    }

    /// <summary>
    /// Gets all monitored providers.
    /// </summary>
    /// <returns>A list of monitored providers.</returns>
    public List<Provider> GetMonitoredProviders()
    {
        return _providers.Values.ToList();
    }

    /// <summary>
    /// Gets the last check time for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>The last check time, or null if not checked yet.</returns>
    public DateTime? GetLastCheckTime(string providerId)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            return null;
        }

        return _lastCheckTimes.TryGetValue(providerId, out var time) ? time : null;
    }

    /// <summary>
    /// Triggers an immediate health check for a specific provider.
    /// </summary>
    /// <param name="providerId">The provider ID to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The health check result.</returns>
    public async Task<ProviderHealthCheckResult> TriggerHealthCheckAsync(
        string providerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));
        }

        _logger?.LogInformation("Triggering immediate health check for provider {ProviderId}", providerId);

        var result = await _healthChecker.CheckHealthAsync(providerId, cancellationToken);
        _lastCheckTimes.AddOrUpdate(providerId, DateTime.UtcNow, (_, _) => DateTime.UtcNow);

        HealthCheckCompleted?.Invoke(this, new HealthCheckCompletedEventArgs { Result = result });

        return result;
    }

    /// <summary>
    /// Triggers immediate health checks for all monitored providers.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A dictionary of provider IDs to their health check results.</returns>
    public async Task<Dictionary<string, ProviderHealthCheckResult>> TriggerAllHealthChecksAsync(
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Triggering immediate health checks for all {Count} providers", _providers.Count);

        var results = await _healthChecker.CheckHealthAsync(_providers.Keys, cancellationToken);

        foreach (var kvp in results)
        {
            _lastCheckTimes.AddOrUpdate(kvp.Key, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
            HealthCheckCompleted?.Invoke(this, new HealthCheckCompletedEventArgs { Result = kvp.Value });
        }

        return results;
    }

    /// <summary>
    /// Gets the health status summary for all monitored providers.
    /// </summary>
    /// <returns>A dictionary of provider IDs to their health statuses.</returns>
    public async Task<Dictionary<string, ProviderHealthStatus>> GetHealthStatusSummaryAsync()
    {
        var summary = new Dictionary<string, ProviderHealthStatus>();

        foreach (var providerId in _providers.Keys)
        {
            var status = await _healthChecker.GetHealthStatusAsync(providerId);
            summary[providerId] = status;
        }

        return summary;
    }

    /// <summary>
    /// Disposes the scheduler and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAsync().GetAwaiter().GetResult();
        _cancellationTokenSource.Dispose();

        _logger?.LogInformation("Health check scheduler disposed");
    }

    private async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Health check scheduler loop started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.CheckInterval, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await RunHealthChecksAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in health check scheduler loop");
            }
        }

        _logger?.LogInformation("Health check scheduler loop stopped");
    }

    private async Task RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        var providersToCheck = _providers.Keys.ToList();

        if (providersToCheck.Count == 0)
        {
            return;
        }

        _logger?.LogDebug("Running scheduled health checks for {Count} providers", providersToCheck.Count);

        try
        {
            var results = await _healthChecker.CheckHealthAsync(providersToCheck, cancellationToken);

            foreach (var kvp in results)
            {
                _lastCheckTimes.AddOrUpdate(kvp.Key, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
                HealthCheckCompleted?.Invoke(this, new HealthCheckCompletedEventArgs { Result = kvp.Value });

                // Log unhealthy providers
                if (kvp.Value.Status != ProviderHealthStatus.Healthy && kvp.Value.Status != ProviderHealthStatus.Unknown)
                {
                    _logger?.LogWarning(
                        "Provider {ProviderId} health check: Status={Status}, SuccessRate={SuccessRate:F2}%, Latency={Latency}ms",
                        kvp.Key,
                        kvp.Value.Status,
                        kvp.Value.SuccessRate,
                        kvp.Value.AverageLatencyMs);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error running scheduled health checks");
        }
    }
}

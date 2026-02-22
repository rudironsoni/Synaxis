// <copyright file="HealthCheckScheduler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Routing.Health;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Synaxis.Routing.SmartRouter;

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
    /// Gets a value indicating whether gets whether the scheduler is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the number of providers being monitored.
    /// </summary>
    public int MonitoredProviderCount => this._providers.Count;

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
        ArgumentNullException.ThrowIfNull(healthChecker);
        this._healthChecker = healthChecker;
        this._options = options ?? new HealthCheckOptions();
        this._providers = new ConcurrentDictionary<string, Provider>(StringComparer.Ordinal);
        this._lastCheckTimes = new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);
        this._logger = logger;
        this._cancellationTokenSource = new CancellationTokenSource();

        // Subscribe to health status changes
        if (this._healthChecker is ProviderHealthMonitor monitor)
        {
            monitor.HealthStatusChanged += (sender, args) =>
            {
                this.HealthStatusChanged?.Invoke(this, args);
            };
        }
    }

    /// <summary>
    /// Starts the health check scheduler.
    /// </summary>
    public void Start()
    {
        if (this._disposed)
        {
            throw new ObjectDisposedException(nameof(HealthCheckScheduler));
        }

        if (this.IsRunning)
        {
            this._logger?.LogWarning("Health check scheduler is already running");
            return;
        }

        this.IsRunning = true;
        this._schedulerTask = this.RunSchedulerAsync(this._cancellationTokenSource.Token);

        this._logger?.LogInformation("Health check scheduler started with interval {Interval}", this._options.CheckInterval);
    }

    /// <summary>
    /// Stops the health check scheduler.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        if (!this.IsRunning)
        {
            return;
        }

        this.IsRunning = false;
        await this._cancellationTokenSource.CancelAsync().ConfigureAwait(false);

        if (this._schedulerTask != null)
        {
            await this._schedulerTask.ConfigureAwait(false);
        }

        this._logger?.LogInformation("Health check scheduler stopped");
    }

    /// <summary>
    /// Adds a provider to monitor.
    /// </summary>
    /// <param name="provider">The provider to monitor.</param>
    public void AddProvider(Provider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (string.IsNullOrEmpty(provider.Id))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(provider));
        }

        this._providers.AddOrUpdate(provider.Id, provider, (_, _) => provider);
        this._logger?.LogInformation("Added provider {ProviderId} to health monitoring", provider.Id);
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

        this._providers.TryRemove(providerId, out _);
        this._lastCheckTimes.TryRemove(providerId, out _);

        this._logger?.LogInformation("Removed provider {ProviderId} from health monitoring", providerId);
    }

    /// <summary>
    /// Gets all monitored providers.
    /// </summary>
    /// <returns>A list of monitored providers.</returns>
    public IList<Provider> GetMonitoredProviders()
    {
        return this._providers.Values.ToList();
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

        return this._lastCheckTimes.TryGetValue(providerId, out var time) ? time : null;
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

        this._logger?.LogInformation("Triggering immediate health check for provider {ProviderId}", providerId);

        var result = await this._healthChecker.CheckHealthAsync(providerId, cancellationToken).ConfigureAwait(false);
        this._lastCheckTimes.AddOrUpdate(providerId, DateTime.UtcNow, (_, _) => DateTime.UtcNow);

        this.HealthCheckCompleted?.Invoke(this, new HealthCheckCompletedEventArgs { Result = result });

        return result;
    }

    /// <summary>
    /// Triggers immediate health checks for all monitored providers.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A dictionary of provider IDs to their health check results.</returns>
    public async Task<IReadOnlyDictionary<string, ProviderHealthCheckResult>> TriggerAllHealthChecksAsync(
        CancellationToken cancellationToken = default)
    {
        this._logger?.LogInformation("Triggering immediate health checks for all {Count} providers", this._providers.Count);

        var results = await this._healthChecker.CheckHealthAsync(this._providers.Keys, cancellationToken).ConfigureAwait(false);

        foreach (var kvp in results)
        {
            this._lastCheckTimes.AddOrUpdate(kvp.Key, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
            this.HealthCheckCompleted?.Invoke(this, new HealthCheckCompletedEventArgs { Result = kvp.Value });
        }

        return results;
    }

    /// <summary>
    /// Gets the health status summary for all monitored providers.
    /// </summary>
    /// <returns>A dictionary of provider IDs to their health statuses.</returns>
    public async Task<IDictionary<string, ProviderHealthStatus>> GetHealthStatusSummaryAsync()
    {
        var summary = new Dictionary<string, ProviderHealthStatus>(StringComparer.Ordinal);

        foreach (var providerId in this._providers.Keys)
        {
            var status = await this._healthChecker.GetHealthStatusAsync(providerId).ConfigureAwait(false);
            summary[providerId] = status;
        }

        return summary;
    }

    /// <summary>
    /// Disposes the scheduler and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;
        _ = this.StopAsync();
        this._cancellationTokenSource.Dispose();

        this._logger?.LogInformation("Health check scheduler disposed");
    }

    private async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        this._logger?.LogInformation("Health check scheduler loop started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(this._options.CheckInterval, cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await this.RunHealthChecksAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                this._logger?.LogError(ex, "Error in health check scheduler loop");
            }
        }

        this._logger?.LogInformation("Health check scheduler loop stopped");
    }

    private async Task RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        var providersToCheck = this._providers.Keys.ToList();

        if (providersToCheck.Count == 0)
        {
            return;
        }

        this._logger?.LogDebug("Running scheduled health checks for {Count} providers", providersToCheck.Count);

        try
        {
            var results = await this._healthChecker.CheckHealthAsync(providersToCheck, cancellationToken).ConfigureAwait(false);

            foreach (var kvp in results)
            {
                this._lastCheckTimes.AddOrUpdate(kvp.Key, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
                this.HealthCheckCompleted?.Invoke(this, new HealthCheckCompletedEventArgs { Result = kvp.Value });

                // Log unhealthy providers
                if (kvp.Value.Status != ProviderHealthStatus.Healthy && kvp.Value.Status != ProviderHealthStatus.Unknown)
                {
                    this._logger?.LogWarning(
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
            this._logger?.LogError(ex, "Error running scheduled health checks");
        }
    }
}

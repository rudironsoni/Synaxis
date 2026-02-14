using System.Collections.Concurrent;

namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Represents performance metrics for a specific provider.
/// </summary>
public class ProviderPerformanceMetrics
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of requests made to this provider.
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
    /// Gets or sets the success rate as a percentage (0.0 to 100.0).
    /// </summary>
    public double SuccessRate => TotalRequests == 0 ? 0.0 : (double)SuccessfulRequests / TotalRequests * 100.0;

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the P50 latency (median) in milliseconds.
    /// </summary>
    public double P50LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the P95 latency in milliseconds.
    /// </summary>
    public double P95LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the P99 latency in milliseconds.
    /// </summary>
    public double P99LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the total input tokens processed.
    /// </summary>
    public long TotalInputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total output tokens processed.
    /// </summary>
    public long TotalOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total cost incurred.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Gets or sets the average cost per request.
    /// </summary>
    public decimal AverageCostPerRequest => TotalRequests == 0 ? 0 : TotalCost / TotalRequests;

    /// <summary>
    /// Gets or sets the timestamp of the last request.
    /// </summary>
    public DateTime? LastRequestTime { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last successful request.
    /// </summary>
    public DateTime? LastSuccessTime { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last failure.
    /// </summary>
    public DateTime? LastFailureTime { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when metrics were last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Tracks performance metrics for AI providers.
/// </summary>
public class ProviderPerformanceTracker
{
    private readonly ConcurrentDictionary<string, ProviderPerformanceMetrics> _metrics;
    private readonly ConcurrentDictionary<string, List<int>> _latencyHistory;
    private readonly ConcurrentDictionary<string, List<DateTime>> _requestTimestamps;
    private readonly object _lock = new();
    private readonly int _maxHistorySize = 1000;
    private readonly int _maxRecentRequests = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderPerformanceTracker"/> class.
    /// </summary>
    public ProviderPerformanceTracker()
    {
        _metrics = new ConcurrentDictionary<string, ProviderPerformanceMetrics>();
        _latencyHistory = new ConcurrentDictionary<string, List<int>>();
        _requestTimestamps = new ConcurrentDictionary<string, List<DateTime>>();
    }

    /// <summary>
    /// Records a request to a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="success">Whether the request was successful.</param>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    /// <param name="inputTokens">The number of input tokens used.</param>
    /// <param name="outputTokens">The number of output tokens used.</param>
    /// <param name="cost">The cost of the request.</param>
    public void RecordRequest(
        string providerId,
        bool success,
        int latencyMs,
        int inputTokens,
        int outputTokens,
        decimal cost)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));
        }

        var metrics = _metrics.GetOrAdd(providerId, id => new ProviderPerformanceMetrics { ProviderId = id });
        var latencyList = _latencyHistory.GetOrAdd(providerId, _ => new List<int>());
        var timestampList = _requestTimestamps.GetOrAdd(providerId, _ => new List<DateTime>());

        lock (_lock)
        {
            metrics.TotalRequests++;
            metrics.TotalInputTokens += inputTokens;
            metrics.TotalOutputTokens += outputTokens;
            metrics.TotalCost += cost;
            metrics.LastRequestTime = DateTime.UtcNow;
            metrics.LastUpdated = DateTime.UtcNow;

            if (success)
            {
                metrics.SuccessfulRequests++;
                metrics.LastSuccessTime = DateTime.UtcNow;
                metrics.ConsecutiveFailures = 0;
            }
            else
            {
                metrics.FailedRequests++;
                metrics.LastFailureTime = DateTime.UtcNow;
                metrics.ConsecutiveFailures++;
            }

            // Track latency
            latencyList.Add(latencyMs);
            if (latencyList.Count > _maxHistorySize)
            {
                latencyList.RemoveAt(0);
            }

            // Track request timestamps for rate limiting
            timestampList.Add(DateTime.UtcNow);
            if (timestampList.Count > _maxRecentRequests)
            {
                timestampList.RemoveAt(0);
            }

            // Update latency statistics
            UpdateLatencyStatistics(metrics, latencyList);
        }
    }

    /// <summary>
    /// Gets the performance metrics for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>The performance metrics, or null if the provider is not tracked.</returns>
    public ProviderPerformanceMetrics? GetMetrics(string providerId)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            return null;
        }

        return _metrics.TryGetValue(providerId, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets all provider metrics.
    /// </summary>
    /// <returns>A dictionary of provider IDs to their metrics.</returns>
    public Dictionary<string, ProviderPerformanceMetrics> GetAllMetrics()
    {
        return _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Gets the recent request rate for a provider (requests per minute).
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>The request rate in requests per minute.</returns>
    public double GetRequestRatePerMinute(string providerId)
    {
        if (string.IsNullOrEmpty(providerId) || !_requestTimestamps.TryGetValue(providerId, out var timestamps))
        {
            return 0.0;
        }

        lock (_lock)
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            var recentRequests = timestamps.Count(t => t > oneMinuteAgo);
            return recentRequests;
        }
    }

    /// <summary>
    /// Resets the metrics for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    public void ResetMetrics(string providerId)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            return;
        }

        _metrics.TryRemove(providerId, out _);
        _latencyHistory.TryRemove(providerId, out _);
        _requestTimestamps.TryRemove(providerId, out _);
    }

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    public void ResetAll()
    {
        _metrics.Clear();
        _latencyHistory.Clear();
        _requestTimestamps.Clear();
    }

    /// <summary>
    /// Gets the health status of a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>The health status.</returns>
    public ProviderHealthStatus GetHealthStatus(string providerId)
    {
        var metrics = GetMetrics(providerId);
        if (metrics == null)
        {
            return ProviderHealthStatus.Unknown;
        }

        if (metrics.TotalRequests == 0)
        {
            return ProviderHealthStatus.Unknown;
        }

        if (metrics.ConsecutiveFailures >= 5)
        {
            return ProviderHealthStatus.Unhealthy;
        }

        if (metrics.SuccessRate < 50.0)
        {
            return ProviderHealthStatus.Degraded;
        }

        if (metrics.SuccessRate < 90.0)
        {
            return ProviderHealthStatus.Warning;
        }

        return ProviderHealthStatus.Healthy;
    }

    private void UpdateLatencyStatistics(ProviderPerformanceMetrics metrics, List<int> latencyList)
    {
        if (latencyList.Count == 0)
        {
            return;
        }

        var sorted = latencyList.OrderBy(x => x).ToList();
        metrics.AverageLatencyMs = latencyList.Average();
        metrics.P50LatencyMs = sorted[sorted.Count / 2];
        metrics.P95LatencyMs = sorted[(int)(sorted.Count * 0.95)];
        metrics.P99LatencyMs = sorted[(int)(sorted.Count * 0.99)];
    }
}

/// <summary>
/// Represents the health status of a provider.
/// </summary>
public enum ProviderHealthStatus
{
    /// <summary>
    /// The provider health is unknown (no data).
    /// </summary>
    Unknown,

    /// <summary>
    /// The provider is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// The provider has some issues but is still functional.
    /// </summary>
    Warning,

    /// <summary>
    /// The provider is degraded.
    /// </summary>
    Degraded,

    /// <summary>
    /// The provider is unhealthy and should not be used.
    /// </summary>
    Unhealthy
}

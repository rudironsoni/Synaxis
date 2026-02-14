using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Synaxis.Api.Health.Layers;

/// <summary>
/// Layer 4: Business Health Check
/// Monitors business-level metrics and SLO compliance including:
/// - Request success rate
/// - Response time percentiles
/// - Error rate
/// - Throughput
/// - Custom business metrics
/// </summary>
public class BusinessHealthCheck : IHealthCheck
{
    private readonly ILogger<BusinessHealthCheck> _logger;
    private readonly BusinessMetrics _metrics;

    public BusinessHealthCheck(
        ILogger<BusinessHealthCheck> logger,
        BusinessMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = new List<string>();
            var data = new Dictionary<string, object>();

            // Get current metrics
            var currentMetrics = _metrics.GetCurrentMetrics();
            data["total_requests"] = currentMetrics.TotalRequests;
            data["successful_requests"] = currentMetrics.SuccessfulRequests;
            data["failed_requests"] = currentMetrics.FailedRequests;
            data["success_rate_percent"] = currentMetrics.SuccessRate;
            data["error_rate_percent"] = currentMetrics.ErrorRate;
            data["avg_response_time_ms"] = currentMetrics.AverageResponseTime;
            data["p50_response_time_ms"] = currentMetrics.P50ResponseTime;
            data["p95_response_time_ms"] = currentMetrics.P95ResponseTime;
            data["p99_response_time_ms"] = currentMetrics.P99ResponseTime;
            data["requests_per_second"] = currentMetrics.RequestsPerSecond;

            // SLO thresholds
            const double minSuccessRate = 99.0; // 99% success rate
            const double maxP95ResponseTime = 500.0; // 500ms P95
            const double maxP99ResponseTime = 1000.0; // 1000ms P99
            const double maxErrorRate = 1.0; // 1% error rate

            // Check SLO compliance
            var sloViolations = new List<string>();

            if (currentMetrics.SuccessRate < minSuccessRate)
            {
                sloViolations.Add($"Success rate {currentMetrics.SuccessRate}% below SLO {minSuccessRate}%");
                issues.Add($"Success rate below threshold: {currentMetrics.SuccessRate}%");
            }

            if (currentMetrics.P95ResponseTime > maxP95ResponseTime)
            {
                sloViolations.Add($"P95 response time {currentMetrics.P95ResponseTime}ms above SLO {maxP95ResponseTime}ms");
                issues.Add($"P95 response time above threshold: {currentMetrics.P95ResponseTime}ms");
            }

            if (currentMetrics.P99ResponseTime > maxP99ResponseTime)
            {
                sloViolations.Add($"P99 response time {currentMetrics.P99ResponseTime}ms above SLO {maxP99ResponseTime}ms");
                issues.Add($"P99 response time above threshold: {currentMetrics.P99ResponseTime}ms");
            }

            if (currentMetrics.ErrorRate > maxErrorRate)
            {
                sloViolations.Add($"Error rate {currentMetrics.ErrorRate}% above SLO {maxErrorRate}%");
                issues.Add($"Error rate above threshold: {currentMetrics.ErrorRate}%");
            }

            data["slo_compliant"] = sloViolations.Count == 0;
            data["slo_violations"] = sloViolations;

            // Check minimum request threshold
            const int minRequestsForSlo = 100;
            if (currentMetrics.TotalRequests < minRequestsForSlo)
            {
                data["slo_status"] = "insufficient_data";
                _logger.LogInformation("Insufficient data for SLO evaluation: {Requests} requests", currentMetrics.TotalRequests);
                return Task.FromResult(HealthCheckResult.Healthy("Insufficient data for SLO evaluation", data));
            }

            // Determine health status
            if (sloViolations.Count > 0)
            {
                _logger.LogWarning("Business health check degraded - SLO violations: {Violations}", string.Join(", ", sloViolations));
                return Task.FromResult(HealthCheckResult.Degraded(
                    "SLO violations detected",
                    data: data,
                    exception: new AggregateException(sloViolations.Select(v => new Exception(v)))));
            }

            _logger.LogInformation("Business health check passed - SLO compliant");
            return Task.FromResult(HealthCheckResult.Healthy("Business metrics are healthy - SLO compliant", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Business health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Business health check failed", exception: ex));
        }
    }
}

/// <summary>
/// Business metrics collector for tracking SLO-relevant metrics
/// </summary>
public class BusinessMetrics
{
    private readonly object _lock = new();
    private readonly List<double> _responseTimes = new();
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private DateTime _windowStart = DateTime.UtcNow;

    public void RecordRequest(bool success, double responseTimeMs)
    {
        lock (_lock)
        {
            _totalRequests++;
            if (success)
            {
                _successfulRequests++;
            }
            else
            {
                _failedRequests++;
            }

            _responseTimes.Add(responseTimeMs);

            // Reset window every 5 minutes
            if ((DateTime.UtcNow - _windowStart).TotalMinutes > 5)
            {
                ResetWindow();
            }
        }
    }

    public BusinessMetricsSnapshot GetCurrentMetrics()
    {
        lock (_lock)
        {
            var sortedTimes = _responseTimes.OrderBy(t => t).ToList();
            var successRate = _totalRequests > 0 ? (double)_successfulRequests / _totalRequests * 100 : 100;
            var errorRate = _totalRequests > 0 ? (double)_failedRequests / _totalRequests * 100 : 0;
            var avgResponseTime = _responseTimes.Count > 0 ? _responseTimes.Average() : 0;
            var p50 = GetPercentile(sortedTimes, 50);
            var p95 = GetPercentile(sortedTimes, 95);
            var p99 = GetPercentile(sortedTimes, 99);
            var windowDuration = (DateTime.UtcNow - _windowStart).TotalSeconds;
            var rps = windowDuration > 0 ? _totalRequests / windowDuration : 0;

            return new BusinessMetricsSnapshot
            {
                TotalRequests = _totalRequests,
                SuccessfulRequests = _successfulRequests,
                FailedRequests = _failedRequests,
                SuccessRate = Math.Round(successRate, 2),
                ErrorRate = Math.Round(errorRate, 2),
                AverageResponseTime = Math.Round(avgResponseTime, 2),
                P50ResponseTime = Math.Round(p50, 2),
                P95ResponseTime = Math.Round(p95, 2),
                P99ResponseTime = Math.Round(p99, 2),
                RequestsPerSecond = Math.Round(rps, 2)
            };
        }
    }

    private double GetPercentile(List<double> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0) return 0;

        var index = (int)Math.Ceiling(sortedValues.Count * percentile / 100.0) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }

    private void ResetWindow()
    {
        _totalRequests = 0;
        _successfulRequests = 0;
        _failedRequests = 0;
        _responseTimes.Clear();
        _windowStart = DateTime.UtcNow;
    }
}

public record BusinessMetricsSnapshot
{
    public long TotalRequests { get; init; }
    public long SuccessfulRequests { get; init; }
    public long FailedRequests { get; init; }
    public double SuccessRate { get; init; }
    public double ErrorRate { get; init; }
    public double AverageResponseTime { get; init; }
    public double P50ResponseTime { get; init; }
    public double P95ResponseTime { get; init; }
    public double P99ResponseTime { get; init; }
    public double RequestsPerSecond { get; init; }
}

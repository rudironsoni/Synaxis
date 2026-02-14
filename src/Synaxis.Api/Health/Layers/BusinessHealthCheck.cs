// <copyright file="BusinessHealthCheck.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Health.Layers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    /// <summary>
    /// Layer 4: Business Health Check
    /// Monitors business-level metrics and SLO compliance including:
    /// - Request success rate
    /// - Response time percentiles
    /// - Error rate
    /// - Throughput
    /// - Custom business metrics.
    /// </summary>
    public class BusinessHealthCheck : IHealthCheck
    {
        private readonly ILogger<BusinessHealthCheck> _logger;
        private readonly BusinessMetrics _metrics;

        public BusinessHealthCheck(
            ILogger<BusinessHealthCheck> logger,
            BusinessMetrics metrics)
        {
            this._logger = logger;
            this._metrics = metrics;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var issues = new List<string>();
                var data = new Dictionary<string, object>(StringComparer.Ordinal);

                var currentMetrics = this._metrics.GetCurrentMetrics();
                AddMetricsData(data, currentMetrics);

                var sloViolations = CheckSloCompliance(currentMetrics, issues);
                data["slo_compliant"] = sloViolations.Count == 0;
                data["slo_violations"] = sloViolations;

                // Check minimum request threshold
                const int minRequestsForSlo = 100;
                if (currentMetrics.TotalRequests < minRequestsForSlo)
                {
                    data["slo_status"] = "insufficient_data";
                    this._logger.LogInformation("Insufficient data for SLO evaluation: {Requests} requests", currentMetrics.TotalRequests);
                    return Task.FromResult(HealthCheckResult.Healthy("Insufficient data for SLO evaluation", data));
                }

                // Determine health status
                if (sloViolations.Count > 0)
                {
                    this._logger.LogWarning("Business health check degraded - SLO violations: {Violations}", string.Join(", ", sloViolations));
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "SLO violations detected",
                        data: data,
                        exception: new AggregateException(sloViolations.Select(v => new Exception(v)))));
                }

                this._logger.LogInformation("Business health check passed - SLO compliant");
                return Task.FromResult(HealthCheckResult.Healthy("Business metrics are healthy - SLO compliant", data));
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Business health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy("Business health check failed", exception: ex));
            }
        }

        private static void AddMetricsData(Dictionary<string, object> data, BusinessMetricsSnapshot currentMetrics)
        {
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
        }

        private static List<string> CheckSloCompliance(BusinessMetricsSnapshot currentMetrics, List<string> issues)
        {
            var sloViolations = new List<string>();

            // SLO thresholds
            const double minSuccessRate = 99.0; // 99% success rate
            const double maxP95ResponseTime = 500.0; // 500ms P95
            const double maxP99ResponseTime = 1000.0; // 1000ms P99
            const double maxErrorRate = 1.0; // 1% error rate

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

            return sloViolations;
        }
    }
}

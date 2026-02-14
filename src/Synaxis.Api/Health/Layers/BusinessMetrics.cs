// <copyright file="BusinessMetrics.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Health.Layers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Business metrics collector for tracking SLO-relevant metrics.
    /// </summary>
    public class BusinessMetrics
    {
        private readonly Lock _lock = new();
        private readonly List<double> _responseTimes = new();
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;
        private DateTime _windowStart = DateTime.UtcNow;

        public void RecordRequest(bool success, double responseTimeMs)
        {
            lock (this._lock)
            {
                this._totalRequests++;
                if (success)
                {
                    this._successfulRequests++;
                }
                else
                {
                    this._failedRequests++;
                }

                this._responseTimes.Add(responseTimeMs);

                // Reset window every 5 minutes
                if ((DateTime.UtcNow - this._windowStart).TotalMinutes > 5)
                {
                    this.ResetWindow();
                }
            }
        }

        public BusinessMetricsSnapshot GetCurrentMetrics()
        {
            lock (this._lock)
            {
                var sortedTimes = this._responseTimes.OrderBy(t => t).ToList();
                var successRate = this._totalRequests > 0 ? (double)this._successfulRequests / this._totalRequests * 100 : 100;
                var errorRate = this._totalRequests > 0 ? (double)this._failedRequests / this._totalRequests * 100 : 0;
                var avgResponseTime = this._responseTimes.Count > 0 ? this._responseTimes.Average() : 0;
                var p50 = GetPercentile(sortedTimes, 50);
                var p95 = GetPercentile(sortedTimes, 95);
                var p99 = GetPercentile(sortedTimes, 99);
                var windowDuration = (DateTime.UtcNow - this._windowStart).TotalSeconds;
                var rps = windowDuration > 0 ? this._totalRequests / windowDuration : 0;

                return new BusinessMetricsSnapshot
                {
                    TotalRequests = this._totalRequests,
                    SuccessfulRequests = this._successfulRequests,
                    FailedRequests = this._failedRequests,
                    SuccessRate = Math.Round(successRate, 2),
                    ErrorRate = Math.Round(errorRate, 2),
                    AverageResponseTime = Math.Round(avgResponseTime, 2),
                    P50ResponseTime = Math.Round(p50, 2),
                    P95ResponseTime = Math.Round(p95, 2),
                    P99ResponseTime = Math.Round(p99, 2),
                    RequestsPerSecond = Math.Round(rps, 2),
                };
            }
        }

        private static double GetPercentile(List<double> sortedValues, int percentile)
        {
            if (sortedValues.Count == 0)
            {
                return 0;
            }

            var index = (int)Math.Ceiling(sortedValues.Count * percentile / 100.0) - 1;
            return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
        }

        private void ResetWindow()
        {
            this._totalRequests = 0;
            this._successfulRequests = 0;
            this._failedRequests = 0;
            this._responseTimes.Clear();
            this._windowStart = DateTime.UtcNow;
        }
    }
}

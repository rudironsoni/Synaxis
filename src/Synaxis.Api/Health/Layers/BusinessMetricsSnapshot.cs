// <copyright file="BusinessMetricsSnapshot.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Health.Layers
{
    /// <summary>
    /// Snapshot of business metrics at a point in time.
    /// </summary>
    public record BusinessMetricsSnapshot
    {
        /// <summary>
        /// Gets the total number of requests.
        /// </summary>
        public long TotalRequests { get; init; }

        /// <summary>
        /// Gets the number of successful requests.
        /// </summary>
        public long SuccessfulRequests { get; init; }

        /// <summary>
        /// Gets the number of failed requests.
        /// </summary>
        public long FailedRequests { get; init; }

        /// <summary>
        /// Gets the success rate as a percentage.
        /// </summary>
        public double SuccessRate { get; init; }

        /// <summary>
        /// Gets the error rate as a percentage.
        /// </summary>
        public double ErrorRate { get; init; }

        /// <summary>
        /// Gets the average response time in milliseconds.
        /// </summary>
        public double AverageResponseTime { get; init; }

        /// <summary>
        /// Gets the 50th percentile response time in milliseconds.
        /// </summary>
        public double P50ResponseTime { get; init; }

        /// <summary>
        /// Gets the 95th percentile response time in milliseconds.
        /// </summary>
        public double P95ResponseTime { get; init; }

        /// <summary>
        /// Gets the 99th percentile response time in milliseconds.
        /// </summary>
        public double P99ResponseTime { get; init; }

        /// <summary>
        /// Gets the requests per second.
        /// </summary>
        public double RequestsPerSecond { get; init; }
    }
}

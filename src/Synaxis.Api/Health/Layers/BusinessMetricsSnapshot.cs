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
}

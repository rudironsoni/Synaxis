// <copyright file="IHealthMonitor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Monitors health of regional infrastructure and external providers.
    /// </summary>
    public interface IHealthMonitor
    {
        /// <summary>
        /// Checks health of a specific region.
        /// </summary>
        /// <param name="region">The region to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the region health status.</returns>
        Task<RegionHealth> CheckRegionHealthAsync(string region);

        /// <summary>
        /// Gets health status for all regions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the dictionary of region health statuses.</returns>
        Task<IDictionary<string, RegionHealth>> GetAllRegionHealthAsync();

        /// <summary>
        /// Checks database connectivity for a region.
        /// </summary>
        /// <param name="region">The region to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether database is healthy.</returns>
        Task<bool> CheckDatabaseHealthAsync(string region);

        /// <summary>
        /// Checks Redis connectivity for a region.
        /// </summary>
        /// <param name="region">The region to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether Redis is healthy.</returns>
        Task<bool> CheckRedisHealthAsync(string region);

        /// <summary>
        /// Checks external provider health (OpenAI, Anthropic, etc.).
        /// </summary>
        /// <param name="provider">The provider to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the provider health status.</returns>
        Task<ProviderHealth> CheckProviderHealthAsync(string provider);

        /// <summary>
        /// Gets aggregate health score for a region (0-100).
        /// </summary>
        /// <param name="region">The region to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the health score.</returns>
        Task<int> GetHealthScoreAsync(string region);

        /// <summary>
        /// Determines if a region is healthy enough to accept traffic.
        /// </summary>
        /// <param name="region">The region to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether region is healthy.</returns>
        Task<bool> IsRegionHealthyAsync(string region);

        /// <summary>
        /// Gets the nearest healthy region to a given region.
        /// </summary>
        /// <param name="fromRegion">The source region.</param>
        /// <param name="availableRegions">The list of available regions to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the nearest healthy region.</returns>
        Task<string> GetNearestHealthyRegionAsync(string fromRegion, IList<string> availableRegions);
    }

    /// <summary>
    /// Represents the health status of a region.
    /// </summary>
    public class RegionHealth
    {
        /// <summary>
        /// Gets or sets the region identifier.
        /// </summary>
        public required string Region { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the region is healthy.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets or sets the health score (0-100).
        /// </summary>
        public int HealthScore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether database is healthy.
        /// </summary>
        public bool DatabaseHealthy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Redis is healthy.
        /// </summary>
        public bool RedisHealthy { get; set; }

        /// <summary>
        /// Gets or sets the provider health statuses.
        /// </summary>
        public IDictionary<string, bool> ProviderHealth { get; set; } = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the timestamp of last health check.
        /// </summary>
        public DateTime LastChecked { get; set; }

        /// <summary>
        /// Gets or sets the status (healthy, degraded, unhealthy).
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// Gets or sets the list of health issues.
        /// </summary>
        public IList<string> Issues { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the response time in milliseconds.
        /// </summary>
        public double ResponseTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the error rate.
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// Gets or sets the number of active connections.
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public required string Version { get; set; }
    }

    /// <summary>
    /// Represents the health status of an external provider.
    /// </summary>
    public class ProviderHealth
    {
        /// <summary>
        /// Gets or sets the provider identifier.
        /// </summary>
        public required string Provider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether provider is available.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Gets or sets the response time in milliseconds.
        /// </summary>
        public int ResponseTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the provider status.
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of last check.
        /// </summary>
        public DateTime LastChecked { get; set; }
    }
}

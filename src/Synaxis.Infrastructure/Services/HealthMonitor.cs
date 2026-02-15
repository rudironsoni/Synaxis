// <copyright file="HealthMonitor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;
    using Synaxis.Core.Contracts;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Monitors health of regional infrastructure and external providers.
    /// </summary>
    public class HealthMonitor : IHealthMonitor
    {
        private readonly SynaxisDbContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HealthMonitor> _logger;

        private static readonly Dictionary<string, (double Latitude, double Longitude)> RegionCoordinates = new(StringComparer.Ordinal)
        {
            { "eu-west-1", (53.3498, -6.2603) },      // Dublin
            { "us-east-1", (39.0438, -77.4874) },     // Virginia
            { "sa-east-1", (-23.5505, -46.6333) }, // São Paulo
        };

        // Cached health status (in-memory, could be moved to Redis for distributed cache)
        private readonly Dictionary<string, RegionHealth> _healthCache = new(StringComparer.Ordinal);
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthMonitor"/> class.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="redis"></param>
        /// <param name="httpClientFactory"></param>
        /// <param name="logger"></param>
        public HealthMonitor(
            SynaxisDbContext context,
            IConnectionMultiplexer redis,
            IHttpClientFactory httpClientFactory,
            ILogger<HealthMonitor> logger)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._redis = redis ?? throw new ArgumentNullException(nameof(redis));
            this._httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<RegionHealth> CheckRegionHealthAsync(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentException("Region cannot be null or empty", nameof(region));
            }

            // Check cache first
            if (this._healthCache.TryGetValue(region, out var cachedHealth) &&
                DateTime.UtcNow - cachedHealth.LastChecked < this._cacheExpiry)
            {
                return cachedHealth;
            }

            try
            {
                var health = new RegionHealth
                {
                    Region = region,
                    LastChecked = DateTime.UtcNow,
                    Issues = new List<string>(),
                };

                // Check database connectivity
                health.DatabaseHealthy = await this.CheckDatabaseHealthAsync(region).ConfigureAwait(false);
                if (!health.DatabaseHealthy)
                {
                    health.Issues.Add("Database connectivity issue");
                }

                // Check Redis connectivity
                health.RedisHealthy = await this.CheckRedisHealthAsync(region).ConfigureAwait(false);
                if (!health.RedisHealthy)
                {
                    health.Issues.Add("Redis connectivity issue");
                }

                // Check external providers
                var providers = new[] { "openai", "anthropic", "google" };
                foreach (var provider in providers)
                {
                    var providerHealth = await this.CheckProviderHealthAsync(provider).ConfigureAwait(false);
                    health.ProviderHealth[provider] = providerHealth.IsAvailable;

                    if (!providerHealth.IsAvailable)
                    {
                        health.Issues.Add($"{provider} unavailable");
                    }
                }

                // Calculate health score
                health.HealthScore = this.CalculateHealthScore(health);
                health.IsHealthy = health.HealthScore >= 70;
                health.Status = this.DetermineHealthStatus(health.HealthScore);

                // Cache the result
                this._healthCache[region] = health;

                this._logger.LogInformation(
                    "Region {Region} health check: {Status} (score: {Score})",
                    region, health.Status, health.HealthScore);

                return health;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error checking health for region {Region}", region);

                return new RegionHealth
                {
                    Region = region,
                    IsHealthy = false,
                    HealthScore = 0,
                    Status = "unhealthy",
                    LastChecked = DateTime.UtcNow,
                    Issues = new List<string> { $"Health check failed: {ex.Message}" },
                };
            }
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, RegionHealth>> GetAllRegionHealthAsync()
        {
            var regions = new[] { "eu-west-1", "us-east-1", "sa-east-1" };
            var healthChecks = await Task.WhenAll(
                regions.Select(r => this.CheckRegionHealthAsync(r))).ConfigureAwait(false);

            return healthChecks.ToDictionary(h => h.Region, h => h, StringComparer.Ordinal);
        }

        /// <inheritdoc/>
        public async Task<bool> CheckDatabaseHealthAsync(string region)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Simple query to check database connectivity
                await this._context.Organizations
                    .Where(o => o.PrimaryRegion == region)
                    .Take(1)
                    .ToListAsync().ConfigureAwait(false);

                stopwatch.Stop();

                var healthy = stopwatch.ElapsedMilliseconds < 1000; // Less than 1 second

                this._logger.LogDebug(
                    "Database health check for {Region}: {Healthy} ({Ms}ms)",
                    region, healthy, stopwatch.ElapsedMilliseconds);

                return healthy;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Database health check failed for region {Region}", region);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CheckRedisHealthAsync(string region)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var db = this._redis.GetDatabase();

                // Ping Redis
                var key = $"health:{region}:ping";
                await db.StringSetAsync(key, DateTime.UtcNow.Ticks.ToString(), TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                var value = await db.StringGetAsync(key).ConfigureAwait(false);

                stopwatch.Stop();

                var healthy = value.HasValue && stopwatch.ElapsedMilliseconds < 500;

                this._logger.LogDebug(
                    "Redis health check for {Region}: {Healthy} ({Ms}ms)",
                    region, healthy, stopwatch.ElapsedMilliseconds);

                return healthy;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Redis health check failed for region {Region}", region);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<ProviderHealth> CheckProviderHealthAsync(string provider)
        {
            var health = new ProviderHealth
            {
                Provider = provider,
                LastChecked = DateTime.UtcNow,
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();
                using var httpClient = this._httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                // Check provider status endpoints (mock for now)
                var statusUrl = this.GetProviderStatusUrl(provider);

                if (string.IsNullOrEmpty(statusUrl))
                {
                    // No status endpoint, assume available
                    health.IsAvailable = true;
                    health.Status = "unknown";
                    health.ResponseTimeMs = 0;
                    return health;
                }

                using var response = await httpClient.GetAsync(statusUrl).ConfigureAwait(false);
                stopwatch.Stop();

                health.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                health.IsAvailable = response.IsSuccessStatusCode;
                health.Status = response.IsSuccessStatusCode ? "operational" : "degraded";

                this._logger.LogDebug(
                    "Provider {Provider} health check: {Status} ({Ms}ms)",
                    provider, health.Status, health.ResponseTimeMs);
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Provider health check failed for {Provider}", provider);
                health.IsAvailable = false;
                health.Status = "unavailable";
                health.ResponseTimeMs = -1;
            }

            return health;
        }

        /// <inheritdoc/>
        public async Task<int> GetHealthScoreAsync(string region)
        {
            var health = await this.CheckRegionHealthAsync(region).ConfigureAwait(false);
            return health.HealthScore;
        }

        /// <inheritdoc/>
        public async Task<bool> IsRegionHealthyAsync(string region)
        {
            var health = await this.CheckRegionHealthAsync(region).ConfigureAwait(false);
            return health.IsHealthy;
        }

        /// <inheritdoc/>
        public async Task<string> GetNearestHealthyRegionAsync(string fromRegion, IList<string> availableRegions)
        {
            if (availableRegions == null || !availableRegions.Any())
            {
                throw new ArgumentException("No available regions provided", nameof(availableRegions));
            }

            // Get health for all available regions
            var healthChecks = await Task.WhenAll(
                availableRegions.Select(r => this.CheckRegionHealthAsync(r))).ConfigureAwait(false);

            // Filter to healthy regions only
            var healthyRegions = healthChecks
                .Where(h => h.IsHealthy)
                .ToList();

            if (!healthyRegions.Any())
            {
                this._logger.LogWarning("No healthy regions available");

                // Return the least unhealthy option
                return healthChecks.OrderByDescending(h => h.HealthScore).First().Region;
            }

            // If fromRegion is healthy and in the list, prefer it
            var fromRegionHealth = healthyRegions.FirstOrDefault(h => string.Equals(h.Region, fromRegion, StringComparison.Ordinal));
            if (fromRegionHealth != null)
            {
                return fromRegion;
            }

            // Find nearest healthy region by geographic distance
            if (!RegionCoordinates.ContainsKey(fromRegion))
            {
                this._logger.LogWarning("Unknown region {Region}, returning first healthy region", fromRegion);
                return healthyRegions.First().Region;
            }

            var fromCoords = RegionCoordinates[fromRegion];
            var nearestRegion = healthyRegions
                .OrderBy(h => this.CalculateDistance(fromCoords, RegionCoordinates[h.Region]))
                .First();

            this._logger.LogInformation(
                "Nearest healthy region to {FromRegion}: {ToRegion}",
                fromRegion, nearestRegion.Region);

            return nearestRegion.Region;
        }

        private int CalculateHealthScore(RegionHealth health)
        {
            var score = 100;

            // Database health: 40 points
            if (!health.DatabaseHealthy)
            {
                score -= 40;
            }

            // Redis health: 30 points
            if (!health.RedisHealthy)
            {
                score -= 30;
            }

            // Provider health: 30 points (10 points each for 3 providers)
            var unavailableProviders = health.ProviderHealth.Count(p => !p.Value);
            score -= unavailableProviders * 10;

            return Math.Max(0, score);
        }

        private string DetermineHealthStatus(int score)
        {
            return score switch
            {
                >= 80 => "healthy",
                >= 50 => "degraded",
                _ => "unhealthy",
            };
        }

        private string GetProviderStatusUrl(string provider)
        {
            // In production, these would be actual status page URLs
            return provider switch
            {
                "openai" => "https://status.openai.com/api/v2/status.json",
                "anthropic" => null, // No public status API
                "google" => null, // No public status API
                _ => null,
            };
        }

        private double CalculateDistance((double Lat, double Lon) from, (double Lat, double Lon) to)
        {
            // Haversine formula for geographic distance
            const double earthRadius = 6371; // km

            var dLat = this.ToRadians(to.Lat - from.Lat);
            var dLon = this.ToRadians(to.Lon - from.Lon);

            var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                    (Math.Cos(this.ToRadians(from.Lat)) * Math.Cos(this.ToRadians(to.Lat)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2));

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}

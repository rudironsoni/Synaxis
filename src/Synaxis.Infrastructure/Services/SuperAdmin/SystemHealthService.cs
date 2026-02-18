// <copyright file="SystemHealthService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for system health operations.
    /// </summary>
    public class SystemHealthService : ISystemHealthService
    {
        private readonly SynaxisDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SystemHealthService> _logger;
        private readonly string _currentRegion;
        private readonly IReadOnlyDictionary<string, string> _regionEndpoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemHealthService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="currentRegion">The current region.</param>
        public SystemHealthService(
            SynaxisDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<SystemHealthService> logger,
            string currentRegion = "us-east-1")
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._currentRegion = currentRegion;

            this._regionEndpoints = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "us-east-1", "https://api-us.synaxis.io" },
                { "eu-west-1", "https://api-eu.synaxis.io" },
                { "sa-east-1", "https://api-br.synaxis.io" },
            };
        }

        /// <inheritdoc/>
        public async Task<SystemHealthOverview> GetSystemHealthOverviewAsync()
        {
            this._logger.LogInformation("Checking system health across regions");

            var healthByRegion = new Dictionary<string, RegionHealth>(StringComparer.Ordinal);
            var alerts = new List<SystemAlert>();

            var localHealth = await this.CheckLocalRegionHealthAsync().ConfigureAwait(false);
            healthByRegion[this._currentRegion] = localHealth;

            var otherRegions = this._regionEndpoints.Keys
                .Where(r => !string.Equals(r, this._currentRegion, StringComparison.Ordinal))
                .ToList();

            var tasks = otherRegions.Select(region => this.CheckRemoteRegionHealthAsync(region));
            var remoteHealthResults = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var health in remoteHealthResults.Where(h => h != null))
            {
                healthByRegion[health.Region] = health;

                if (!health.IsHealthy)
                {
                    alerts.Add(new SystemAlert
                    {
                        Severity = string.Equals(health.Status, "Down", StringComparison.Ordinal) ? "Critical" : "Warning",
                        Message = $"Region {health.Region} is {health.Status}",
                        Region = health.Region,
                        Timestamp = DateTime.UtcNow,
                    });
                }
            }

            var healthyRegions = healthByRegion.Values.Count(h => h.IsHealthy);

            return new SystemHealthOverview
            {
                HealthByRegion = healthByRegion,
                AllRegionsHealthy = healthyRegions == healthByRegion.Count,
                TotalRegions = healthByRegion.Count,
                HealthyRegions = healthyRegions,
                Alerts = alerts,
                CheckedAt = DateTime.UtcNow,
            };
        }

        /// <inheritdoc/>
        public async Task<RegionHealth> CheckLocalRegionHealthAsync()
        {
            var startTime = DateTime.UtcNow;

            try
            {
                await this._context.Organizations.AnyAsync().ConfigureAwait(false);

                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                return new RegionHealth
                {
                    Region = this._currentRegion,
                    IsHealthy = true,
                    Status = "Healthy",
                    ResponseTimeMs = responseTime,
                    ErrorRate = 0,
                    ActiveConnections = 0,
                    Version = "1.0.0",
                    LastChecked = DateTime.UtcNow,
                };
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Health check failed for local region {Region}", this._currentRegion);

                return new RegionHealth
                {
                    Region = this._currentRegion,
                    IsHealthy = false,
                    Status = "Down",
                    ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                    ErrorRate = 1.0,
                    ActiveConnections = 0,
                    Version = "Unknown",
                    LastChecked = DateTime.UtcNow,
                };
            }
        }

        /// <inheritdoc/>
        public async Task<RegionHealth> CheckRemoteRegionHealthAsync(string region)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                using var client = this._httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var endpoint = this._regionEndpoints[region];
                using var response = await client.GetAsync($"{endpoint}/api/health").ConfigureAwait(false);

                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    return new RegionHealth
                    {
                        Region = region,
                        IsHealthy = true,
                        Status = "Healthy",
                        ResponseTimeMs = responseTime,
                        ErrorRate = 0,
                        ActiveConnections = 0,
                        Version = "1.0.0",
                        LastChecked = DateTime.UtcNow,
                    };
                }

                return new RegionHealth
                {
                    Region = region,
                    IsHealthy = false,
                    Status = "Degraded",
                    ResponseTimeMs = responseTime,
                    ErrorRate = 0.5,
                    ActiveConnections = 0,
                    Version = "Unknown",
                    LastChecked = DateTime.UtcNow,
                };
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Health check failed for region {Region}", region);

                return new RegionHealth
                {
                    Region = region,
                    IsHealthy = false,
                    Status = "Down",
                    ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                    ErrorRate = 1.0,
                    ActiveConnections = 0,
                    Version = "Unknown",
                    LastChecked = DateTime.UtcNow,
                };
            }
        }
    }
}

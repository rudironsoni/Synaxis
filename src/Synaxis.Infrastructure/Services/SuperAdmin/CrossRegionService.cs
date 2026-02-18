// <copyright file="CrossRegionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for cross-region operations.
    /// </summary>
    public class CrossRegionService : ICrossRegionService
    {
        private readonly SynaxisDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CrossRegionService> _logger;
        private readonly string _currentRegion;
        private readonly IReadOnlyDictionary<string, string> _regionEndpoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossRegionService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="currentRegion">The current region.</param>
        public CrossRegionService(
            SynaxisDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<CrossRegionService> logger,
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
        public async Task<IList<OrganizationSummary>> GetCrossRegionOrganizationsAsync()
        {
            this._logger.LogInformation("Fetching cross-region organizations");

            var localOrgs = await this.GetLocalOrganizationsAsync().ConfigureAwait(false);

            var otherRegions = this._regionEndpoints.Keys
                .Where(r => !string.Equals(r, this._currentRegion, StringComparison.Ordinal))
                .ToList();

            var tasks = otherRegions.Select(region => this.FetchOrganizationsFromRegionAsync(region));
            var remoteResults = await Task.WhenAll(tasks).ConfigureAwait(false);

            var allOrgs = localOrgs.Concat(remoteResults.SelectMany(r => r)).ToList();

            this._logger.LogInformation(
                "Retrieved {Count} organizations across {Regions} regions",
                allOrgs.Count,
                this._regionEndpoints.Count);

            return allOrgs;
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, RegionUsage>> GetCrossRegionUsageAsync(DateTime start, DateTime end)
        {
            var localUsage = await this.GetLocalUsageAsync(start, end).ConfigureAwait(false);

            var otherRegions = this._regionEndpoints.Keys
                .Where(r => !string.Equals(r, this._currentRegion, StringComparison.Ordinal))
                .ToList();

            var tasks = otherRegions.Select(region => this.FetchUsageFromRegionAsync(region, start, end));
            var remoteResults = await Task.WhenAll(tasks).ConfigureAwait(false);

            var usageByRegion = new Dictionary<string, RegionUsage>(StringComparer.Ordinal)
            {
                { this._currentRegion, localUsage },
            };

            foreach (var result in remoteResults.Where(r => r != null && r.Region != null))
            {
                usageByRegion[result!.Region!] = result;
            }

            IDictionary<string, RegionUsage> resultDictionary = usageByRegion;
            return resultDictionary;
        }

        private Task<List<OrganizationSummary>> GetLocalOrganizationsAsync()
        {
            return this._context.Organizations
                .Select(o => new OrganizationSummary
                {
                    Id = o.Id,
                    Name = o.Name,
                    Slug = o.Slug,
                    PrimaryRegion = o.PrimaryRegion,
                    Tier = o.Tier,
                    UserCount = o.Users.Count(u => u.IsActive),
                    TeamCount = o.Teams.Count(t => t.IsActive),
                    MonthlyRequests = 0,
                    MonthlySpend = o.CreditBalance,
                    IsActive = o.IsActive,
                    CreatedAt = o.CreatedAt,
                })
                .ToListAsync();
        }

        private async Task<List<OrganizationSummary>> FetchOrganizationsFromRegionAsync(string region)
        {
            try
            {
                using var client = this._httpClientFactory.CreateClient();
                var endpoint = this._regionEndpoints[region];
                using var response = await client.GetAsync($"{endpoint}/api/internal/organizations").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonSerializer.Deserialize<List<OrganizationSummary>>(json) ?? new List<OrganizationSummary>();
                }

                this._logger.LogWarning(
                    "Failed to fetch organizations from region {Region}: {Status}",
                    region,
                    response.StatusCode);
                return new List<OrganizationSummary>();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error fetching organizations from region {Region}", region);
                return new List<OrganizationSummary>();
            }
        }

        private async Task<RegionUsage> GetLocalUsageAsync(DateTime start, DateTime end)
        {
            var requests = await this._context.Requests
                .Where(r => r.CreatedAt >= start && r.CreatedAt <= end)
                .ToListAsync()
                .ConfigureAwait(false);

            return new RegionUsage
            {
                Region = this._currentRegion,
                Requests = requests.Count,
                Tokens = requests.Sum(r => r.TotalTokens),
                Spend = requests.Sum(r => r.Cost),
                Organizations = await this._context.Organizations.CountAsync().ConfigureAwait(false),
                Users = await this._context.Users.CountAsync(u => u.IsActive).ConfigureAwait(false),
            };
        }

        private async Task<RegionUsage?> FetchUsageFromRegionAsync(string region, DateTime start, DateTime end)
        {
            try
            {
                using var client = this._httpClientFactory.CreateClient();
                var endpoint = this._regionEndpoints[region];
                using var response = await client.GetAsync(
                    $"{endpoint}/api/internal/usage?start={start:O}&end={end:O}").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonSerializer.Deserialize<RegionUsage?>(json);
                }

                this._logger.LogWarning(
                    "Failed to fetch usage from region {Region}: {Status}",
                    region,
                    response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error fetching usage from region {Region}", region);
                return null;
            }
        }
    }
}

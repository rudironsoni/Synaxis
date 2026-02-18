// <copyright file="GlobalAnalyticsService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for global analytics operations.
    /// </summary>
    public class GlobalAnalyticsService : IGlobalAnalyticsService
    {
        private readonly SynaxisDbContext _context;
        private readonly ICrossRegionService _crossRegionService;
        private readonly ILogger<GlobalAnalyticsService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalAnalyticsService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="crossRegionService">The cross region service.</param>
        /// <param name="logger">The logger.</param>
        public GlobalAnalyticsService(
            SynaxisDbContext context,
            ICrossRegionService crossRegionService,
            ILogger<GlobalAnalyticsService> logger)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._crossRegionService = crossRegionService ?? throw new ArgumentNullException(nameof(crossRegionService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<GlobalUsageAnalytics> GetGlobalUsageAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            this._logger.LogInformation("Fetching global usage analytics from {Start} to {End}", start, end);

            var usageByRegion = await this._crossRegionService.GetCrossRegionUsageAsync(start, end).ConfigureAwait(false);
            var (requestsByModel, requestsByProvider) = await this.GetRequestBreakdownsAsync(start, end).ConfigureAwait(false);
            var (totalOrganizations, totalUsers, activeOrganizations) = await this.GetOrganizationCountsAsync().ConfigureAwait(false);

            var totalRequests = usageByRegion.Values.Sum(u => u.Requests);
            var totalTokens = usageByRegion.Values.Sum(u => u.Tokens);
            var totalSpend = usageByRegion.Values.Sum(u => u.Spend);

            return new GlobalUsageAnalytics
            {
                TotalRequests = totalRequests,
                TotalTokens = totalTokens,
                TotalSpend = totalSpend,
                TotalOrganizations = totalOrganizations,
                TotalUsers = totalUsers,
                ActiveOrganizations = activeOrganizations,
                UsageByRegion = usageByRegion,
                RequestsByModel = requestsByModel,
                RequestsByProvider = requestsByProvider,
                StartDate = start,
                EndDate = end,
            };
        }

        /// <inheritdoc/>
        public async Task<(Dictionary<string, long> RequestsByModel, Dictionary<string, long> RequestsByProvider)> GetRequestBreakdownsAsync(DateTime start, DateTime end)
        {
            var requestsByModel = await this._context.Requests
                .Where(r => r.CreatedAt >= start && r.CreatedAt <= end)
                .GroupBy(r => r.Model)
                .Select(g => new { Model = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Model ?? "unknown", x => (long)x.Count)
                .ConfigureAwait(false);

            var requestsByProvider = await this._context.Requests
                .Where(r => r.CreatedAt >= start && r.CreatedAt <= end)
                .GroupBy(r => r.Provider)
                .Select(g => new { Provider = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Provider ?? "unknown", x => (long)x.Count)
                .ConfigureAwait(false);

            return (RequestsByModel: requestsByModel, RequestsByProvider: requestsByProvider);
        }

        /// <inheritdoc/>
        public async Task<(int TotalOrganizations, int TotalUsers, int ActiveOrganizations)> GetOrganizationCountsAsync()
        {
            var totalOrganizations = await this._context.Organizations.CountAsync().ConfigureAwait(false);
            var totalUsers = await this._context.Users.CountAsync(u => u.IsActive).ConfigureAwait(false);
            var activeOrganizations = await this._context.Organizations.CountAsync(o => o.IsActive).ConfigureAwait(false);

            return (TotalOrganizations: totalOrganizations, TotalUsers: totalUsers, ActiveOrganizations: activeOrganizations);
        }
    }
}

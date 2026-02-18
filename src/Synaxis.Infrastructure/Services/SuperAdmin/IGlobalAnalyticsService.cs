// <copyright file="IGlobalAnalyticsService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Service for global analytics operations.
    /// </summary>
    public interface IGlobalAnalyticsService
    {
        /// <summary>
        /// Gets global usage analytics.
        /// </summary>
        /// <param name="startDate">Start date (defaults to 30 days ago).</param>
        /// <param name="endDate">End date (defaults to now).</param>
        /// <returns>Global usage analytics.</returns>
        Task<GlobalUsageAnalytics> GetGlobalUsageAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets request breakdowns by model and provider.
        /// </summary>
        /// <param name="start">Start date.</param>
        /// <param name="end">End date.</param>
        /// <returns>Requests by model and provider.</returns>
        Task<(Dictionary<string, long> RequestsByModel, Dictionary<string, long> RequestsByProvider)> GetRequestBreakdownsAsync(DateTime start, DateTime end);

        /// <summary>
        /// Gets organization counts.
        /// </summary>
        /// <returns>Total organizations, users, and active organizations.</returns>
        Task<(int TotalOrganizations, int TotalUsers, int ActiveOrganizations)> GetOrganizationCountsAsync();
    }
}

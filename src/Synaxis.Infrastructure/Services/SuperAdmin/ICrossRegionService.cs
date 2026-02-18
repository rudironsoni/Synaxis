// <copyright file="ICrossRegionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Service for cross-region operations.
    /// </summary>
    public interface ICrossRegionService
    {
        /// <summary>
        /// Gets organizations across all regions.
        /// </summary>
        /// <returns>List of organization summaries from all regions.</returns>
        Task<IList<OrganizationSummary>> GetCrossRegionOrganizationsAsync();

        /// <summary>
        /// Gets usage data across all regions.
        /// </summary>
        /// <param name="start">Start date.</param>
        /// <param name="end">End date.</param>
        /// <returns>Aggregated usage by region.</returns>
        Task<IDictionary<string, RegionUsage>> GetCrossRegionUsageAsync(DateTime start, DateTime end);
    }
}

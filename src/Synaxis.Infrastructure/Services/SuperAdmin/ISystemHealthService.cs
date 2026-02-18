// <copyright file="ISystemHealthService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Service for system health operations.
    /// </summary>
    public interface ISystemHealthService
    {
        /// <summary>
        /// Gets the system health overview.
        /// </summary>
        /// <returns>System health overview.</returns>
        Task<SystemHealthOverview> GetSystemHealthOverviewAsync();

        /// <summary>
        /// Checks health of the local region.
        /// </summary>
        /// <returns>Region health status.</returns>
        Task<RegionHealth> CheckLocalRegionHealthAsync();

        /// <summary>
        /// Checks health of a remote region.
        /// </summary>
        /// <param name="region">Region identifier.</param>
        /// <returns>Region health status.</returns>
        Task<RegionHealth> CheckRemoteRegionHealthAsync(string region);
    }
}

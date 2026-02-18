// <copyright file="IComplianceService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Service for compliance operations.
    /// </summary>
    public interface IComplianceService
    {
        /// <summary>
        /// Gets cross-border transfers.
        /// </summary>
        /// <param name="startDate">Start date (defaults to 30 days ago).</param>
        /// <param name="endDate">End date (defaults to now).</param>
        /// <returns>List of cross-border transfers.</returns>
        Task<IList<CrossBorderTransferReport>> GetCrossBorderTransfersAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets the compliance status.
        /// </summary>
        /// <returns>Compliance status dashboard.</returns>
        Task<ComplianceStatusDashboard> GetComplianceStatusAsync();
    }
}

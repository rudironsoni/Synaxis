// <copyright file="IUsageRecordRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories
{
    using Billing.Domain.Entities;

    /// <summary>
    /// Repository interface for usage record operations.
    /// </summary>
    public interface IUsageRecordRepository
    {
        /// <summary>
        /// Gets a usage record by its ID.
        /// </summary>
        /// <param name="id">The usage record ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The usage record or null if not found.</returns>
        Task<UsageRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all usage records for an organization within a date range.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="fromDate">The start date.</param>
        /// <param name="toDate">The end date.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of usage records.</returns>
        Task<IReadOnlyList<UsageRecord>> GetByOrganizationAsync(
            Guid organizationId,
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new usage record.
        /// </summary>
        /// <param name="usageRecord">The usage record to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task AddAsync(UsageRecord usageRecord, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple usage records in bulk.
        /// </summary>
        /// <param name="usageRecords">The usage records to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task AddRangeAsync(IEnumerable<UsageRecord> usageRecords, CancellationToken cancellationToken = default);
    }
}

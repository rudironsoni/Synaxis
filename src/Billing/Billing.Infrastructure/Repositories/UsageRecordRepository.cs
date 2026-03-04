// <copyright file="UsageRecordRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories
{
    using Billing.Domain.Entities;
    using Billing.Infrastructure.Data;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Repository implementation for usage record operations.
    /// </summary>
    public class UsageRecordRepository : IUsageRecordRepository
    {
        private readonly BillingDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsageRecordRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public UsageRecordRepository(BillingDbContext context)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public Task<UsageRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return this._context.UsageRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<UsageRecord>> GetByOrganizationAsync(
            Guid organizationId,
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default)
        {
            return await this._context.UsageRecords
                .Where(u => u.OrganizationId == organizationId && u.Timestamp >= fromDate && u.Timestamp <= toDate)
                .OrderByDescending(u => u.Timestamp)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddAsync(UsageRecord usageRecord, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(usageRecord);
            _ = await this._context.UsageRecords.AddAsync(usageRecord, cancellationToken).ConfigureAwait(false);
            _ = await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddRangeAsync(IEnumerable<UsageRecord> usageRecords, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(usageRecords);
            await this._context.UsageRecords.AddRangeAsync(usageRecords, cancellationToken).ConfigureAwait(false);
            _ = await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

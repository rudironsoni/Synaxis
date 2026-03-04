// <copyright file="SubscriptionRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories
{
    using Billing.Domain.Entities;
    using Billing.Infrastructure.Data;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Repository implementation for subscription operations.
    /// </summary>
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly BillingDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public SubscriptionRepository(BillingDbContext context)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return this._context.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Subscription?> GetActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return this._context.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Status == "Active", cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Subscription>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return await this._context.Subscriptions
                .Where(s => s.OrganizationId == organizationId)
                .OrderByDescending(s => s.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Subscription>> GetExpiredAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
        {
            return await this._context.Subscriptions
                .Where(s => s.Status == "Active" && s.EndDate < asOfDate)
                .OrderBy(s => s.EndDate)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(subscription);
            _ = await this._context.Subscriptions.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
            _ = await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(subscription);
            _ = this._context.Subscriptions.Update(subscription);
            _ = await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

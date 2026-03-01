// <copyright file="PaymentRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories
{
    using Billing.Domain.Entities;
    using Billing.Infrastructure.Data;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Repository implementation for payment operations.
    /// </summary>
    public class PaymentRepository : IPaymentRepository
    {
        private readonly BillingDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public PaymentRepository(BillingDbContext context)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return this._context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            return this._context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Payment>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return await this._context.Payments
                .Where(p => p.OrganizationId == organizationId)
                .OrderByDescending(p => p.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Payment>> GetPendingAsync(CancellationToken cancellationToken = default)
        {
            return await this._context.Payments
                .Where(p => p.Status == "Pending")
                .OrderBy(p => p.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(payment);
            _ = await this._context.Payments.AddAsync(payment, cancellationToken).ConfigureAwait(false);
            _ = await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(payment);
            _ = this._context.Payments.Update(payment);
            _ = await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

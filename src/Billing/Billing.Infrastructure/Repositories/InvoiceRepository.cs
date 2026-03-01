// <copyright file="InvoiceRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories
{
    using Billing.Domain.Entities;
    using Billing.Infrastructure.Data;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Repository implementation for invoice operations.
    /// </summary>
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly BillingDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public InvoiceRepository(BillingDbContext context)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return this._context.Invoices
                .Include(i => i.LineItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
        {
            return this._context.Invoices
                .Include(i => i.LineItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Invoice>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return await this._context.Invoices
                .Where(i => i.OrganizationId == organizationId)
                .OrderByDescending(i => i.IssueDate)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Invoice>> GetOverdueAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
        {
            return await this._context.Invoices
                .Where(i => i.Status != "Paid" && i.DueDate < asOfDate)
                .OrderBy(i => i.DueDate)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(invoice);
            _ = await this._context.Invoices.AddAsync(invoice, cancellationToken).ConfigureAwait(false);
            _ = await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(invoice);
            _ = this._context.Invoices.Update(invoice);
            _ = await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

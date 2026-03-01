// <copyright file="IInvoiceRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories
{
    using Billing.Domain.Entities;

    /// <summary>
    /// Repository interface for invoice operations.
    /// </summary>
    public interface IInvoiceRepository
    {
        /// <summary>
        /// Gets an invoice by its ID.
        /// </summary>
        /// <param name="id">The invoice ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The invoice or null if not found.</returns>
        Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an invoice by its invoice number.
        /// </summary>
        /// <param name="invoiceNumber">The invoice number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The invoice or null if not found.</returns>
        Task<Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all invoices for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of invoices.</returns>
        Task<IReadOnlyList<Invoice>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all overdue invoices as of a specific date.
        /// </summary>
        /// <param name="asOfDate">The date to check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of overdue invoices.</returns>
        Task<IReadOnlyList<Invoice>> GetOverdueAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new invoice.
        /// </summary>
        /// <param name="invoice">The invoice to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing invoice.
        /// </summary>
        /// <param name="invoice">The invoice to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    }
}

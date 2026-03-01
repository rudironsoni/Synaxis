// <copyright file="IPaymentRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories
{
    using Billing.Domain.Entities;

    /// <summary>
    /// Repository interface for payment operations.
    /// </summary>
    public interface IPaymentRepository
    {
        /// <summary>
        /// Gets a payment by its ID.
        /// </summary>
        /// <param name="id">The payment ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The payment or null if not found.</returns>
        Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a payment by its transaction ID.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The payment or null if not found.</returns>
        Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all payments for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of payments.</returns>
        Task<IReadOnlyList<Payment>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all pending payments.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of pending payments.</returns>
        Task<IReadOnlyList<Payment>> GetPendingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new payment.
        /// </summary>
        /// <param name="payment">The payment to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing payment.
        /// </summary>
        /// <param name="payment">The payment to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
    }
}

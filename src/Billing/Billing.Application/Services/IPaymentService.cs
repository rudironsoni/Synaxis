// <copyright file="IPaymentService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.Services
{
    using Billing.Application.DTOs;

    /// <summary>
    /// Service interface for payment operations.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Processes a payment.
        /// </summary>
        /// <param name="request">The payment request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The payment result.</returns>
        Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a payment by its ID.
        /// </summary>
        /// <param name="paymentId">The payment ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The payment DTO or null if not found.</returns>
        Task<PaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all payments for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of payment DTOs.</returns>
        Task<IReadOnlyList<PaymentDto>> GetOrganizationPaymentsAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refunds a payment.
        /// </summary>
        /// <param name="paymentId">The payment ID.</param>
        /// <param name="amount">The amount to refund.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if refunded; false if payment not found or not eligible for refund.</returns>
        Task<bool> RefundPaymentAsync(Guid paymentId, decimal amount, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Request to process a payment.
    /// </summary>
    /// <param name="OrganizationId">The organization ID.</param>
    /// <param name="InvoiceId">Optional associated invoice ID.</param>
    /// <param name="Amount">The payment amount.</param>
    /// <param name="Currency">The currency code.</param>
    /// <param name="PaymentMethod">The payment method.</param>
    /// <param name="PaymentToken">Optional payment token.</param>
    public record ProcessPaymentRequest(
        Guid OrganizationId,
        Guid? InvoiceId,
        decimal Amount,
        string Currency,
        string PaymentMethod,
        string? PaymentToken = null);

    /// <summary>
    /// Result of a payment operation.
    /// </summary>
    /// <param name="Success">Whether the payment succeeded.</param>
    /// <param name="PaymentId">The payment ID if successful.</param>
    /// <param name="TransactionId">The transaction ID.</param>
    /// <param name="Status">The payment status.</param>
    /// <param name="ErrorMessage">Error message if failed.</param>
    public record PaymentResultDto(
        bool Success,
        Guid? PaymentId,
        string TransactionId,
        string Status,
        string? ErrorMessage);

    /// <summary>
    /// Data transfer object for a payment.
    /// </summary>
    /// <param name="Id">The payment ID.</param>
    /// <param name="OrganizationId">The organization ID.</param>
    /// <param name="InvoiceId">Optional associated invoice ID.</param>
    /// <param name="TransactionId">The transaction ID.</param>
    /// <param name="Amount">The payment amount.</param>
    /// <param name="Currency">The currency code.</param>
    /// <param name="Status">The payment status.</param>
    /// <param name="PaymentMethod">The payment method.</param>
    /// <param name="CreatedAt">The creation timestamp.</param>
    public record PaymentDto(
        Guid Id,
        Guid OrganizationId,
        Guid? InvoiceId,
        string TransactionId,
        decimal Amount,
        string Currency,
        string Status,
        string PaymentMethod,
        DateTime CreatedAt);
}

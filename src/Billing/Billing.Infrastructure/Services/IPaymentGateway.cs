// <copyright file="IPaymentGateway.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Services
{
    /// <summary>
    /// Interface for payment gateway operations.
    /// </summary>
    public interface IPaymentGateway
    {
        /// <summary>
        /// Processes a payment request.
        /// </summary>
        /// <param name="request">The payment request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The payment result.</returns>
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refunds a payment.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="amount">The amount to refund.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The payment result.</returns>
        Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a webhook payload from the payment gateway.
        /// </summary>
        /// <param name="payload">The webhook payload.</param>
        /// <param name="signature">The webhook signature.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if valid; otherwise false.</returns>
        Task<bool> ValidateWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
    }
}

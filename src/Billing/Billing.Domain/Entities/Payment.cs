// <copyright file="Payment.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Entities;

/// <summary>
/// Represents a payment transaction for an organization.
/// </summary>
public class Payment
{
    /// <summary>
    /// Gets or sets the unique identifier for the payment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the organization ID that owns this payment.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the associated invoice ID, if any.
    /// </summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>
    /// Gets or sets the transaction ID from the payment gateway.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., USD, EUR).
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the status of the payment (Pending, Completed, Failed, Refunded).
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Gets or sets the payment method (CreditCard, BankTransfer, etc.).
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw gateway response.
    /// </summary>
    public string? GatewayResponse { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }
}

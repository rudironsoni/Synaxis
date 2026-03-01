// <copyright file="Invoice.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Entities;

/// <summary>
/// Represents an invoice for an organization.
/// </summary>
public class Invoice
{
    /// <summary>
    /// Gets or sets the unique identifier for the invoice.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the organization ID that owns this invoice.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the invoice number.
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date the invoice was issued.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the due date for the invoice.
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Gets or sets the total amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., USD, EUR).
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the status of the invoice (Pending, Paid, Overdue, Cancelled).
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Gets or sets the description of the invoice.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the line items included in the invoice.
    /// </summary>
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the payment timestamp.
    /// </summary>
    public DateTime? PaidAt { get; set; }
}

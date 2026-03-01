// <copyright file="InvoiceLineItem.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Entities;

/// <summary>
/// Represents a line item in an invoice.
/// </summary>
public class InvoiceLineItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the line item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the invoice ID this line item belongs to.
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Gets or sets the description of the line item.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the total amount for this line item.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the resource type.
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Gets or sets the resource ID.
    /// </summary>
    public string? ResourceId { get; set; }
}

// <copyright file="Invoice.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Monthly invoice for organization.
    /// </summary>
    public class Invoice
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the organization navigation property.
        /// </summary>
        public virtual Organization Organization { get; set; }

        /// <summary>
        /// Gets or sets the invoice number (e.g., INV-2026-02-001).
        /// </summary>
        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Gets or sets the invoice status: draft, issued, paid, overdue, cancelled.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the billing period start date.
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Gets or sets the billing period end date.
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Gets or sets the total amount in USD (base currency).
        /// </summary>
        public decimal TotalAmountUsd { get; set; }

        /// <summary>
        /// Gets or sets the total amount in organization's billing currency.
        /// </summary>
        public decimal TotalAmountBillingCurrency { get; set; }

        /// <summary>
        /// Gets or sets the organization's billing currency.
        /// </summary>
        [StringLength(3)]
        public string BillingCurrency { get; set; }

        /// <summary>
        /// Gets or sets the exchange rate used for conversion.
        /// </summary>
        public decimal ExchangeRate { get; set; }

        /// <summary>
        /// Gets or sets the breakdown by service/model.
        /// </summary>
        public IDictionary<string, decimal> LineItems { get; set; } = new Dictionary<string, decimal>();

        /// <summary>
        /// Gets or sets the due date for payment.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Gets or sets the date invoice was paid.
        /// </summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

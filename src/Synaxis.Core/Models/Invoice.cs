using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Monthly invoice for organization
    /// </summary>
    public class Invoice
    {
        public Guid Id { get; set; }
        
        public Guid OrganizationId { get; set; }
        
        public virtual Organization Organization { get; set; }
        
        /// <summary>
        /// Invoice number (e.g., INV-2026-02-001)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; }
        
        /// <summary>
        /// Invoice status: draft, issued, paid, overdue, cancelled
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Status { get; set; }
        
        /// <summary>
        /// Billing period start
        /// </summary>
        public DateTime PeriodStart { get; set; }
        
        /// <summary>
        /// Billing period end
        /// </summary>
        public DateTime PeriodEnd { get; set; }
        
        /// <summary>
        /// Total amount in USD (base currency)
        /// </summary>
        public decimal TotalAmountUsd { get; set; }
        
        /// <summary>
        /// Total amount in organization's billing currency
        /// </summary>
        public decimal TotalAmountBillingCurrency { get; set; }
        
        /// <summary>
        /// Organization's billing currency
        /// </summary>
        [StringLength(3)]
        public string BillingCurrency { get; set; }
        
        /// <summary>
        /// Exchange rate used for conversion
        /// </summary>
        public decimal ExchangeRate { get; set; }
        
        /// <summary>
        /// Breakdown by service/model
        /// </summary>
        public Dictionary<string, decimal> LineItems { get; set; } = new Dictionary<string, decimal>();
        
        /// <summary>
        /// Due date for payment
        /// </summary>
        public DateTime? DueDate { get; set; }
        
        /// <summary>
        /// Date invoice was paid
        /// </summary>
        public DateTime? PaidAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

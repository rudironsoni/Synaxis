// <copyright file="CreditTransaction.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Tracks credit balance changes for billing.
    /// </summary>
    public class CreditTransaction
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
        public virtual Organization? Organization { get; set; }

        /// <summary>
        /// Gets or sets the transaction type: topup, charge, refund, adjustment.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the amount in USD (base currency).
        /// </summary>
        public decimal AmountUsd { get; set; }

        /// <summary>
        /// Gets or sets the balance before transaction (USD).
        /// </summary>
        public decimal BalanceBeforeUsd { get; set; }

        /// <summary>
        /// Gets or sets the balance after transaction (USD).
        /// </summary>
        public decimal BalanceAfterUsd { get; set; }

        /// <summary>
        /// Gets or sets the description of the transaction.
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reference to related spend log, invoice, etc.
        /// </summary>
        public Guid? ReferenceId { get; set; }

        /// <summary>
        /// Gets or sets the user who initiated the transaction (for top-ups).
        /// </summary>
        public Guid? InitiatedBy { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

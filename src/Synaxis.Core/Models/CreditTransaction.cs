using System;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Tracks credit balance changes for billing
    /// </summary>
    public class CreditTransaction
    {
        public Guid Id { get; set; }
        
        public Guid OrganizationId { get; set; }
        
        public virtual Organization Organization { get; set; }
        
        /// <summary>
        /// Transaction type: topup, charge, refund, adjustment
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; }
        
        /// <summary>
        /// Amount in USD (base currency)
        /// </summary>
        public decimal AmountUsd { get; set; }
        
        /// <summary>
        /// Balance before transaction (USD)
        /// </summary>
        public decimal BalanceBeforeUsd { get; set; }
        
        /// <summary>
        /// Balance after transaction (USD)
        /// </summary>
        public decimal BalanceAfterUsd { get; set; }
        
        /// <summary>
        /// Description of the transaction
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }
        
        /// <summary>
        /// Reference to related spend log, invoice, etc.
        /// </summary>
        public Guid? ReferenceId { get; set; }
        
        /// <summary>
        /// User who initiated the transaction (for top-ups)
        /// </summary>
        public Guid? InitiatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

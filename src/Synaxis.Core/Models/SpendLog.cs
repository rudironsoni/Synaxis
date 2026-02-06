using System;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Tracks spending and usage for billing
    /// </summary>
    public class SpendLog
    {
        public Guid Id { get; set; }
        
        public Guid OrganizationId { get; set; }
        
        public virtual Organization Organization { get; set; }
        
        public Guid? TeamId { get; set; }
        
        public virtual Team Team { get; set; }
        
        public Guid? VirtualKeyId { get; set; }
        
        public virtual VirtualKey VirtualKey { get; set; }
        
        public Guid? RequestId { get; set; }
        
        /// <summary>
        /// Amount in USD (base currency)
        /// </summary>
        public decimal AmountUsd { get; set; }
        
        /// <summary>
        /// Model used for the request
        /// </summary>
        [StringLength(100)]
        public string Model { get; set; }
        
        /// <summary>
        /// Provider name
        /// </summary>
        [StringLength(100)]
        public string Provider { get; set; }
        
        /// <summary>
        /// Number of tokens used
        /// </summary>
        public int Tokens { get; set; }
        
        /// <summary>
        /// Region where the spend occurred
        /// </summary>
        [StringLength(50)]
        public string Region { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

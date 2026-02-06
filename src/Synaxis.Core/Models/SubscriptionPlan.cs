using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Subscription plan template with limits configuration
    /// </summary>
    public class SubscriptionPlan
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Slug { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public decimal? MonthlyPriceUsd { get; set; }
        
        public decimal? YearlyPriceUsd { get; set; }
        
        /// <summary>
        /// Limits configuration as JSON
        /// </summary>
        public Dictionary<string, object> LimitsConfig { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Feature flags
        /// </summary>
        public Dictionary<string, object> Features { get; set; } = new Dictionary<string, object>();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

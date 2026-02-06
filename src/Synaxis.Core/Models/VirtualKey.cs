using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// API Key with budget and rate limiting
    /// </summary>
    public class VirtualKey
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string KeyHash { get; set; }
        
        public Guid OrganizationId { get; set; }
        
        public virtual Organization Organization { get; set; }
        
        public Guid TeamId { get; set; }
        
        public virtual Team Team { get; set; }
        
        public Guid CreatedBy { get; set; }
        
        public virtual User Creator { get; set; }
        
        [StringLength(255)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsRevoked { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        
        public string RevokedReason { get; set; }
        
        /// <summary>
        /// Maximum budget for this key (NULL = inherit from team)
        /// </summary>
        public decimal? MaxBudget { get; set; }
        
        /// <summary>
        /// Current spend against budget
        /// </summary>
        public decimal CurrentSpend { get; set; } = 0.00m;
        
        /// <summary>
        /// Requests per minute limit (NULL = inherit from team)
        /// </summary>
        public int? RpmLimit { get; set; }
        
        /// <summary>
        /// Tokens per minute limit (NULL = inherit from team)
        /// </summary>
        public int? TpmLimit { get; set; }
        
        /// <summary>
        /// Allowed models (NULL = all models allowed)
        /// </summary>
        public List<string> AllowedModels { get; set; }
        
        /// <summary>
        /// Blocked models
        /// </summary>
        public List<string> BlockedModels { get; set; }
        
        /// <summary>
        /// Key expiration date
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
        
        public List<string> Tags { get; set; } = new List<string>();
        
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Region for database partitioning
        /// </summary>
        [Required]
        public string UserRegion { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Requests made with this virtual key
        /// </summary>
        public virtual ICollection<Request> Requests { get; set; }
        
        /// <summary>
        /// Check if key is expired
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
        
        /// <summary>
        /// Check if key has exceeded budget
        /// </summary>
        public bool IsOverBudget => MaxBudget.HasValue && CurrentSpend >= MaxBudget.Value;
        
        /// <summary>
        /// Calculate remaining budget
        /// </summary>
        public decimal? RemainingBudget => MaxBudget.HasValue ? MaxBudget.Value - CurrentSpend : null;
    }
}

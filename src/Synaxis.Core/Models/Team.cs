using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Team within an organization
    /// </summary>
    public class Team
    {
        public Guid Id { get; set; }
        
        public Guid OrganizationId { get; set; }
        
        public virtual Organization Organization { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Slug { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Monthly budget for all team keys (NULL = no limit)
        /// </summary>
        public decimal? MonthlyBudget { get; set; }
        
        /// <summary>
        /// Alert threshold (percentage of budget)
        /// </summary>
        public decimal BudgetAlertThreshold { get; set; } = 80.00m;
        
        /// <summary>
        /// Allowed models (NULL = inherit from org)
        /// </summary>
        public List<string> AllowedModels { get; set; }
        
        /// <summary>
        /// Blocked models
        /// </summary>
        public List<string> BlockedModels { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<TeamMembership> TeamMemberships { get; set; }
        
        public virtual ICollection<VirtualKey> VirtualKeys { get; set; }
    }
}

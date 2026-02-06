using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Root tenant entity. All data is scoped to an organization.
    /// </summary>
    public class Organization
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Slug { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        /// <summary>
        /// Primary region where organization was created
        /// </summary>
        [Required]
        public string PrimaryRegion { get; set; }
        
        /// <summary>
        /// Available regions for this organization
        /// </summary>
        public List<string> AvailableRegions { get; set; } = new List<string> { "eu-west-1", "us-east-1", "sa-east-1" };
        
        /// <summary>
        /// Subscription tier: free, pro, enterprise
        /// </summary>
        public string Tier { get; set; } = "free";
        
        /// <summary>
        /// Billing currency: USD, EUR, BRL, GBP
        /// </summary>
        public string BillingCurrency { get; set; } = "USD";
        
        /// <summary>
        /// Current credit balance for overage charges
        /// </summary>
        public decimal CreditBalance { get; set; } = 0.00m;
        
        public string CreditCurrency { get; set; } = "USD";
        
        public string SubscriptionStatus { get; set; } = "active";
        
        public DateTime? SubscriptionStartedAt { get; set; }
        
        public DateTime? SubscriptionExpiresAt { get; set; }
        
        public bool IsTrial { get; set; }
        
        public DateTime? TrialStartedAt { get; set; }
        
        public DateTime? TrialEndsAt { get; set; }
        
        // Resource Quotas (NULL = use tier defaults)
        public int? MaxTeams { get; set; }
        
        public int? MaxUsersPerTeam { get; set; }
        
        public int? MaxKeysPerUser { get; set; }
        
        public int? MaxConcurrentRequests { get; set; }
        
        public long? MonthlyRequestLimit { get; set; }
        
        public long? MonthlyTokenLimit { get; set; }
        
        public int DataRetentionDays { get; set; } = 30;
        
        public bool RequireSso { get; set; }
        
        public List<string> AllowedEmailDomains { get; set; } = new List<string>();
        
        /// <summary>
        /// Privacy consent tracking (GDPR/LGPD)
        /// </summary>
        public Dictionary<string, object> PrivacyConsent { get; set; } = new Dictionary<string, object>();
        
        public DateTime? TermsAcceptedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsVerified { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Team> Teams { get; set; }
        
        public virtual ICollection<User> Users { get; set; }
        
        public virtual ICollection<VirtualKey> VirtualKeys { get; set; }
        
        public virtual ICollection<Request> Requests { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// User account with data residency tracking for GDPR/LGPD compliance
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }
        
        public Guid OrganizationId { get; set; }
        
        public virtual Organization Organization { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }
        
        public DateTime? EmailVerifiedAt { get; set; }
        
        [Required]
        public string PasswordHash { get; set; }
        
        /// <summary>
        /// Region where user data must be stored (GDPR/LGPD compliance)
        /// </summary>
        [Required]
        public string DataResidencyRegion { get; set; }
        
        /// <summary>
        /// Region where user account was created
        /// </summary>
        [Required]
        public string CreatedInRegion { get; set; }
        
        [StringLength(100)]
        public string FirstName { get; set; }
        
        [StringLength(100)]
        public string LastName { get; set; }
        
        public string AvatarUrl { get; set; }
        
        [StringLength(50)]
        public string Timezone { get; set; } = "UTC";
        
        [StringLength(10)]
        public string Locale { get; set; } = "en-US";
        
        /// <summary>
        /// Role: owner, admin, member, viewer
        /// </summary>
        public string Role { get; set; } = "member";
        
        /// <summary>
        /// Privacy consent tracking (GDPR/LGPD)
        /// </summary>
        public Dictionary<string, object> PrivacyConsent { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Has user consented to cross-border data transfers
        /// </summary>
        public bool CrossBorderConsentGiven { get; set; }
        
        public DateTime? CrossBorderConsentDate { get; set; }
        
        public string CrossBorderConsentVersion { get; set; }
        
        public bool MfaEnabled { get; set; }
        
        public string MfaSecret { get; set; }
        
        public DateTime? LastLoginAt { get; set; }
        
        public int FailedLoginAttempts { get; set; }
        
        public DateTime? LockedUntil { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<TeamMembership> TeamMemberships { get; set; }
        
        public virtual ICollection<VirtualKey> VirtualKeys { get; set; }
        
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}

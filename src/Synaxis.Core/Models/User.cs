// <copyright file="User.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// User account with data residency tracking for GDPR/LGPD compliance.
    /// </summary>
    public class User
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
        /// Gets or sets the email address.
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the email verification timestamp.
        /// </summary>
        public DateTime? EmailVerifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the password hash.
        /// </summary>
        [Required]
        public string PasswordHash { get; set; }

        /// <summary>
        /// Gets or sets the region where user data must be stored (GDPR/LGPD compliance).
        /// </summary>
        [Required]
        public string DataResidencyRegion { get; set; }

        /// <summary>
        /// Gets or sets the region where user account was created.
        /// </summary>
        [Required]
        public string CreatedInRegion { get; set; }

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        public string AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timezone.
        /// </summary>
        [StringLength(50)]
        public string Timezone { get; set; } = "UTC";

        /// <summary>
        /// Gets or sets the locale.
        /// </summary>
        [StringLength(10)]
        public string Locale { get; set; } = "en-US";

        /// <summary>
        /// Gets or sets the role: owner, admin, member, viewer.
        /// </summary>
        public string Role { get; set; } = "member";

        /// <summary>
        /// Gets or sets the privacy consent tracking (GDPR/LGPD).
        /// </summary>
        public IDictionary<string, object> PrivacyConsent { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets a value indicating whether user has consented to cross-border data transfers.
        /// </summary>
        public bool CrossBorderConsentGiven { get; set; }

        /// <summary>
        /// Gets or sets the cross-border consent date.
        /// </summary>
        public DateTime? CrossBorderConsentDate { get; set; }

        /// <summary>
        /// Gets or sets the cross-border consent version.
        /// </summary>
        public string CrossBorderConsentVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether MFA is enabled.
        /// </summary>
        public bool MfaEnabled { get; set; }

        /// <summary>
        /// Gets or sets the MFA secret.
        /// </summary>
        public string MfaSecret { get; set; }

        /// <summary>
        /// Gets or sets the last login timestamp.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Gets or sets the number of failed login attempts.
        /// </summary>
        public int FailedLoginAttempts { get; set; }

        /// <summary>
        /// Gets or sets the account lock expiration timestamp.
        /// </summary>
        public DateTime? LockedUntil { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user account is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the team memberships navigation property.
        /// </summary>
        public virtual ICollection<TeamMembership> TeamMemberships { get; set; }

        /// <summary>
        /// Gets or sets the virtual keys navigation property.
        /// </summary>
        public virtual ICollection<VirtualKey> VirtualKeys { get; set; }

        /// <summary>
        /// Gets the full name of the user.
        /// </summary>
        public string FullName => $"{this.FirstName} {this.LastName}".Trim();
    }
}

// <copyright file="PasswordPolicy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Password policy configuration for an organization.
    /// </summary>
    public class PasswordPolicy
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        [Required]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the minimum password length.
        /// </summary>
        [Range(8, 128)]
        public int MinLength { get; set; } = 12;

        /// <summary>
        /// Gets or sets a value indicating whether password must contain uppercase letters.
        /// </summary>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether password must contain lowercase letters.
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether password must contain numbers.
        /// </summary>
        public bool RequireNumbers { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether password must contain special characters.
        /// </summary>
        public bool RequireSpecialCharacters { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of previous passwords to remember for history check.
        /// </summary>
        [Range(0, 24)]
        public int PasswordHistoryCount { get; set; } = 5;

        /// <summary>
        /// Gets or sets the password expiration period in days (0 = never expires).
        /// </summary>
        [Range(0, 365)]
        public int PasswordExpirationDays { get; set; } = 90;

        /// <summary>
        /// Gets or sets the number of days before expiration to show warning.
        /// </summary>
        [Range(0, 30)]
        public int PasswordExpirationWarningDays { get; set; } = 14;

        /// <summary>
        /// Gets or sets the maximum number of failed password change attempts before lockout.
        /// </summary>
        [Range(3, 10)]
        public int MaxFailedChangeAttempts { get; set; } = 5;

        /// <summary>
        /// Gets or sets the lockout duration in minutes after failed attempts.
        /// </summary>
        [Range(5, 60)]
        public int LockoutDurationMinutes { get; set; } = 15;

        /// <summary>
        /// Gets or sets a value indicating whether common passwords are blocked.
        /// </summary>
        public bool BlockCommonPasswords { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether user info (email, name) is blocked in password.
        /// </summary>
        public bool BlockUserInfoInPassword { get; set; } = true;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the organization navigation property.
        /// </summary>
        public virtual Organization Organization { get; set; }
    }
}

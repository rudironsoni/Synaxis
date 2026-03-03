// <copyright file="OrganizationRequests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers.Organizations
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request to create a new organization.
    /// </summary>
    public class CreateOrganizationRequest
    {
        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the primary region.
        /// </summary>
        public string? PrimaryRegion { get; set; }
    }

    /// <summary>
    /// Request to update an organization.
    /// </summary>
    public class UpdateOrganizationRequest
    {
        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        public string? Slug { get; set; }

        /// <summary>
        /// Gets or sets the organization description.
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request to update organization limits.
    /// </summary>
    public class UpdateOrganizationLimitsRequest
    {
        /// <summary>
        /// Gets or sets the maximum number of teams.
        /// </summary>
        public int? MaxTeams { get; set; }

        /// <summary>
        /// Gets or sets the maximum users per team.
        /// </summary>
        public int? MaxUsersPerTeam { get; set; }

        /// <summary>
        /// Gets or sets the maximum keys per user.
        /// </summary>
        public int? MaxKeysPerUser { get; set; }

        /// <summary>
        /// Gets or sets the maximum concurrent requests.
        /// </summary>
        public int? MaxConcurrentRequests { get; set; }

        /// <summary>
        /// Gets or sets the monthly request limit.
        /// </summary>
        public int? MonthlyRequestLimit { get; set; }

        /// <summary>
        /// Gets or sets the monthly token limit.
        /// </summary>
        public int? MonthlyTokenLimit { get; set; }
    }

    /// <summary>
    /// Request to update organization settings.
    /// </summary>
    public class UpdateOrganizationSettingsRequest
    {
        /// <summary>
        /// Gets or sets the data retention days.
        /// </summary>
        public int? DataRetentionDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSO is required.
        /// </summary>
        public bool? RequireSso { get; set; }

        /// <summary>
        /// Gets or sets the allowed email domains.
        /// </summary>
        public IList<string>? AllowedEmailDomains { get; set; }
    }

    /// <summary>
    /// Request to update password policy.
    /// </summary>
    public class UpdatePasswordPolicyRequest
    {
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
    }

    /// <summary>
    /// Request to create a new organization API key.
    /// </summary>
    public class CreateOrganizationApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        [StringLength(255)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public IDictionary<string, object>? Permissions { get; set; }

        /// <summary>
        /// Gets or sets the expiration timestamp (null = never expires).
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Request to update an organization API key.
    /// </summary>
    public class UpdateOrganizationApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        [StringLength(255)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public IDictionary<string, object>? Permissions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to revoke the key.
        /// </summary>
        public bool? Revoke { get; set; }

        /// <summary>
        /// Gets or sets the revocation reason.
        /// </summary>
        [StringLength(500)]
        public string? RevokedReason { get; set; }
    }
}

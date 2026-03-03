// <copyright file="OrganizationResponses.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers.Organizations
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Organization response model.
    /// </summary>
    public class OrganizationResponse
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the primary region.
        /// </summary>
        public string PrimaryRegion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the subscription tier.
        /// </summary>
        public string Tier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the billing currency.
        /// </summary>
        public string BillingCurrency { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the credit balance.
        /// </summary>
        public decimal CreditBalance { get; set; }

        /// <summary>
        /// Gets or sets the subscription status.
        /// </summary>
        public string SubscriptionStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the organization is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the organization is verified.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Organization summary response for list endpoints.
    /// </summary>
    public class OrganizationSummaryResponse
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the subscription tier.
        /// </summary>
        public string Tier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the organization is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Organization limits response.
    /// </summary>
    public class OrganizationLimitsResponse
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
    /// Organization settings response.
    /// </summary>
    public class OrganizationSettingsResponse
    {
        /// <summary>
        /// Gets or sets the subscription tier.
        /// </summary>
        public string Tier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data retention days.
        /// </summary>
        public int DataRetentionDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSO is required.
        /// </summary>
        public bool RequireSso { get; set; }

        /// <summary>
        /// Gets or sets the allowed email domains.
        /// </summary>
        public IList<string>? AllowedEmailDomains { get; set; }

        /// <summary>
        /// Gets or sets the available regions.
        /// </summary>
        public IList<string>? AvailableRegions { get; set; }

        /// <summary>
        /// Gets or sets the privacy consent.
        /// </summary>
        public IDictionary<string, object>? PrivacyConsent { get; set; }
    }

    /// <summary>
    /// Password policy response model.
    /// </summary>
    public class PasswordPolicyResponse
    {
        /// <summary>
        /// Gets or sets the password policy ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the minimum password length.
        /// </summary>
        public int MinLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether password must contain uppercase letters.
        /// </summary>
        public bool RequireUppercase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether password must contain lowercase letters.
        /// </summary>
        public bool RequireLowercase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether password must contain numbers.
        /// </summary>
        public bool RequireNumbers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether password must contain special characters.
        /// </summary>
        public bool RequireSpecialCharacters { get; set; }

        /// <summary>
        /// Gets or sets the number of previous passwords to remember for history check.
        /// </summary>
        public int PasswordHistoryCount { get; set; }

        /// <summary>
        /// Gets or sets the password expiration period in days (0 = never expires).
        /// </summary>
        public int PasswordExpirationDays { get; set; }

        /// <summary>
        /// Gets or sets the number of days before expiration to show warning.
        /// </summary>
        public int PasswordExpirationWarningDays { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of failed password change attempts before lockout.
        /// </summary>
        public int MaxFailedChangeAttempts { get; set; }

        /// <summary>
        /// Gets or sets the lockout duration in minutes after failed attempts.
        /// </summary>
        public int LockoutDurationMinutes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether common passwords are blocked.
        /// </summary>
        public bool BlockCommonPasswords { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user info (email, name) is blocked in password.
        /// </summary>
        public bool BlockUserInfoInPassword { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Response for creating an organization API key.
    /// </summary>
    public class CreateOrganizationApiKeyResponse
    {
        /// <summary>
        /// Gets or sets the API key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the actual API key value (shown only once).
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key prefix.
        /// </summary>
        public string KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public IDictionary<string, object> Permissions { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the key is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Response for organization API key details.
    /// </summary>
    public class OrganizationApiKeyResponse
    {
        /// <summary>
        /// Gets or sets the API key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key prefix.
        /// </summary>
        public string KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public IDictionary<string, object> Permissions { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the last used timestamp.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Gets or sets the revocation timestamp.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the key is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the creator information.
        /// </summary>
        public OrganizationApiKeyCreatorInfo? CreatedBy { get; set; }
    }

    /// <summary>
    /// Creator information for organization API keys.
    /// </summary>
    public class OrganizationApiKeyCreatorInfo
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string? LastName { get; set; }
    }

    /// <summary>
    /// Response for rotating an organization API key.
    /// </summary>
    public class RotateOrganizationApiKeyResponse
    {
        /// <summary>
        /// Gets or sets the API key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new API key value (shown only once).
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new key prefix.
        /// </summary>
        public string KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public IDictionary<string, object> Permissions { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the key is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the rotation timestamp.
        /// </summary>
        public DateTime RotatedAt { get; set; }
    }

    /// <summary>
    /// Response for organization API key usage statistics.
    /// </summary>
    public class OrganizationApiKeyUsageResponse
    {
        /// <summary>
        /// Gets or sets the API key ID.
        /// </summary>
        public Guid ApiKeyId { get; set; }

        /// <summary>
        /// Gets or sets the total number of requests made with this key.
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets the number of error requests.
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the error rate (0.0 to 1.0).
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// Gets or sets the last used timestamp.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Gets or sets the requests by hour.
        /// </summary>
        public IDictionary<string, int> RequestsByHour { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Gets or sets the requests by day.
        /// </summary>
        public IDictionary<string, int> RequestsByDay { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Gets or sets the requests by week.
        /// </summary>
        public IDictionary<string, int> RequestsByWeek { get; set; } = new Dictionary<string, int>();
    }
}

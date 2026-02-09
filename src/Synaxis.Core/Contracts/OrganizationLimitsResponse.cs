// <copyright file="OrganizationLimitsResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System.Collections.Generic;

    /// <summary>
    /// Organization limits DTO.
    /// </summary>
    public class OrganizationLimitsResponse
    {
        /// <summary>
        /// Gets or sets the maximum teams.
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
        public long? MonthlyRequestLimit { get; set; }

        /// <summary>
        /// Gets or sets the monthly token limit.
        /// </summary>
        public long? MonthlyTokenLimit { get; set; }
    }

    /// <summary>
    /// Request to update organization limits.
    /// </summary>
    public class UpdateOrganizationLimitsRequest
    {
        /// <summary>
        /// Gets or sets the maximum teams.
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
        public long? MonthlyRequestLimit { get; set; }

        /// <summary>
        /// Gets or sets the monthly token limit.
        /// </summary>
        public long? MonthlyTokenLimit { get; set; }
    }

    /// <summary>
    /// Organization settings response DTO.
    /// </summary>
    public class OrganizationSettingsResponse
    {
        /// <summary>
        /// Gets or sets the tier.
        /// </summary>
        public required string Tier { get; set; }

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
        public IList<string> AllowedEmailDomains { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the available regions.
        /// </summary>
        public IList<string> AvailableRegions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the privacy consent.
        /// </summary>
        public IDictionary<string, object> PrivacyConsent { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
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
        public IList<string> AllowedEmailDomains { get; set; } = new List<string>();
    }
}

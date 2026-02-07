// <copyright file="Organization.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Root tenant entity. All data is scoped to an organization.
    /// </summary>
    public class Organization
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Slug { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the organization description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the primary region where organization was created.
        /// </summary>
        [Required]
        public string PrimaryRegion { get; set; }

        /// <summary>
        /// Gets or sets the available regions for this organization.
        /// </summary>
        public IList<string> AvailableRegions { get; set; } = new List<string> { "eu-west-1", "us-east-1", "sa-east-1" };

        /// <summary>
        /// Gets or sets the subscription tier: free, pro, enterprise.
        /// </summary>
        public string Tier { get; set; } = "free";

        /// <summary>
        /// Gets or sets the billing currency: USD, EUR, BRL, GBP.
        /// </summary>
        public string BillingCurrency { get; set; } = "USD";

        /// <summary>
        /// Gets or sets the current credit balance for overage charges.
        /// </summary>
        public decimal CreditBalance { get; set; } = 0.00m;

        /// <summary>
        /// Gets or sets the credit currency.
        /// </summary>
        public string CreditCurrency { get; set; } = "USD";

        /// <summary>
        /// Gets or sets the subscription status.
        /// </summary>
        public string SubscriptionStatus { get; set; } = "active";

        /// <summary>
        /// Gets or sets the subscription start date.
        /// </summary>
        public DateTime? SubscriptionStartedAt { get; set; }

        /// <summary>
        /// Gets or sets the subscription expiration date.
        /// </summary>
        public DateTime? SubscriptionExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a trial subscription.
        /// </summary>
        public bool IsTrial { get; set; }

        /// <summary>
        /// Gets or sets the trial start date.
        /// </summary>
        public DateTime? TrialStartedAt { get; set; }

        /// <summary>
        /// Gets or sets the trial end date.
        /// </summary>
        public DateTime? TrialEndsAt { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of teams (NULL = use tier defaults).
        /// </summary>
        public int? MaxTeams { get; set; }

        /// <summary>
        /// Gets or sets the maximum users per team (NULL = use tier defaults).
        /// </summary>
        public int? MaxUsersPerTeam { get; set; }

        /// <summary>
        /// Gets or sets the maximum keys per user (NULL = use tier defaults).
        /// </summary>
        public int? MaxKeysPerUser { get; set; }

        /// <summary>
        /// Gets or sets the maximum concurrent requests (NULL = use tier defaults).
        /// </summary>
        public int? MaxConcurrentRequests { get; set; }

        /// <summary>
        /// Gets or sets the monthly request limit (NULL = use tier defaults).
        /// </summary>
        public long? MonthlyRequestLimit { get; set; }

        /// <summary>
        /// Gets or sets the monthly token limit (NULL = use tier defaults).
        /// </summary>
        public long? MonthlyTokenLimit { get; set; }

        /// <summary>
        /// Gets or sets the data retention period in days.
        /// </summary>
        public int DataRetentionDays { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether SSO is required.
        /// </summary>
        public bool RequireSso { get; set; }

        /// <summary>
        /// Gets or sets the allowed email domains.
        /// </summary>
        public IList<string> AllowedEmailDomains { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the privacy consent tracking (GDPR/LGPD).
        /// </summary>
        public IDictionary<string, object> PrivacyConsent { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the terms acceptance date.
        /// </summary>
        public DateTime? TermsAcceptedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the organization is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the organization is verified.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the teams navigation property.
        /// </summary>
        public virtual ICollection<Team> Teams { get; set; }

        /// <summary>
        /// Gets or sets the users navigation property.
        /// </summary>
        public virtual ICollection<User> Users { get; set; }

        /// <summary>
        /// Gets or sets the virtual keys navigation property.
        /// </summary>
        public virtual ICollection<VirtualKey> VirtualKeys { get; set; }

        /// <summary>
        /// Gets or sets the requests navigation property.
        /// </summary>
        public virtual ICollection<Request> Requests { get; set; }
    }
}

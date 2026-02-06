// <copyright file="ITenantService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Service for managing organizations (tenants).
    /// </summary>
    public interface ITenantService
    {
        /// <summary>
        /// Create a new organization.
        /// </summary>
        /// <param name="request">The organization creation request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created organization.</returns>
        Task<Organization> CreateOrganizationAsync(CreateOrganizationRequest request);

        /// <summary>
        /// Get organization by ID.
        /// </summary>
        /// <param name="id">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the organization.</returns>
        Task<Organization> GetOrganizationAsync(Guid id);

        /// <summary>
        /// Get organization by slug.
        /// </summary>
        /// <param name="slug">The organization slug.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the organization.</returns>
        Task<Organization> GetOrganizationBySlugAsync(string slug);

        /// <summary>
        /// Update organization settings.
        /// </summary>
        /// <param name="id">The unique identifier of the organization.</param>
        /// <param name="request">The organization update request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated organization.</returns>
        Task<Organization> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request);

        /// <summary>
        /// Delete organization (soft delete).
        /// </summary>
        /// <param name="id">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
        Task<bool> DeleteOrganizationAsync(Guid id);

        /// <summary>
        /// Get organization limits (merged from plan + overrides).
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the organization limits.</returns>
        Task<OrganizationLimits> GetOrganizationLimitsAsync(Guid organizationId);

        /// <summary>
        /// Check if organization can add more teams.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether teams can be added.</returns>
        Task<bool> CanAddTeamAsync(Guid organizationId);

        /// <summary>
        /// Check if organization has reached concurrent request limit.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether under limit.</returns>
        Task<bool> IsUnderConcurrentLimitAsync(Guid organizationId);
    }

    /// <summary>
    /// Represents a request to create an organization.
    /// </summary>
    public class CreateOrganizationRequest
    {
        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        public string Slug { get; set; }

        /// <summary>
        /// Gets or sets the primary region.
        /// </summary>
        public string PrimaryRegion { get; set; }

        /// <summary>
        /// Gets or sets the billing currency.
        /// </summary>
        public string BillingCurrency { get; set; } = "USD";
    }

    /// <summary>
    /// Represents a request to update an organization.
    /// </summary>
    public class UpdateOrganizationRequest
    {
        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the organization description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the organization settings.
        /// </summary>
        public IDictionary<string, object> Settings { get; set; }
    }

    /// <summary>
    /// Represents limits for an organization.
    /// </summary>
    public class OrganizationLimits
    {
        /// <summary>
        /// Gets or sets the maximum number of teams.
        /// </summary>
        public int MaxTeams { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of users per team.
        /// </summary>
        public int MaxUsersPerTeam { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of keys per user.
        /// </summary>
        public int MaxKeysPerUser { get; set; }

        /// <summary>
        /// Gets or sets the maximum concurrent requests.
        /// </summary>
        public int MaxConcurrentRequests { get; set; }

        /// <summary>
        /// Gets or sets the monthly request limit.
        /// </summary>
        public long MonthlyRequestLimit { get; set; }

        /// <summary>
        /// Gets or sets the monthly token limit.
        /// </summary>
        public long MonthlyTokenLimit { get; set; }

        /// <summary>
        /// Gets or sets the data retention period in days.
        /// </summary>
        public int DataRetentionDays { get; set; }
    }
}

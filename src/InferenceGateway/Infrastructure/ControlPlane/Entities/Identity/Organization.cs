// <copyright file="Organization.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity
{
    using Synaxis.InferenceGateway.Infrastructure.Data.Interfaces;

    /// <summary>
    /// Represents an organization (tenant) in the identity schema.
    /// </summary>
    public class Organization : ISoftDeletable
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the legal name.
        /// </summary>
        public required string LegalName { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public required string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the URL slug.
        /// </summary>
        public required string Slug { get; set; }

        /// <summary>
        /// Gets or sets the registration number.
        /// </summary>
        public string? RegistrationNumber { get; set; }

        /// <summary>
        /// Gets or sets the tax ID.
        /// </summary>
        public string? TaxId { get; set; }

        /// <summary>
        /// Gets or sets the legal address.
        /// </summary>
        public string? LegalAddress { get; set; }

        /// <summary>
        /// Gets or sets the primary contact email.
        /// </summary>
        public string? PrimaryContactEmail { get; set; }

        /// <summary>
        /// Gets or sets the billing email.
        /// </summary>
        public string? BillingEmail { get; set; }

        /// <summary>
        /// Gets or sets the support email.
        /// </summary>
        public string? SupportEmail { get; set; }

        /// <summary>
        /// Gets or sets the phone number.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the industry.
        /// </summary>
        public string? Industry { get; set; }

        /// <summary>
        /// Gets or sets the company size.
        /// </summary>
        public string? CompanySize { get; set; }

        /// <summary>
        /// Gets or sets the website URL.
        /// </summary>
        public string? WebsiteUrl { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public required string Status { get; set; } = "Active";

        /// <summary>
        /// Gets or sets the plan tier.
        /// </summary>
        public required string PlanTier { get; set; } = "Free";

        /// <summary>
        /// Gets or sets the trial end date.
        /// </summary>
        public DateTime? TrialEndsAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether MFA is required.
        /// </summary>
        public bool RequireMfa { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the creator ID.
        /// </summary>
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last updater ID.
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the deletion timestamp.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Gets or sets the deleter ID.
        /// </summary>
        public Guid? DeletedBy { get; set; }

        /// <summary>
        /// Gets or sets the organization settings.
        /// </summary>
        public OrganizationSettings? Settings { get; set; }

        /// <summary>
        /// Gets or sets the groups.
        /// </summary>
        public ICollection<Group> Groups { get; set; } = new List<Group>();

        /// <summary>
        /// Gets or sets the user memberships.
        /// </summary>
        public ICollection<UserOrganizationMembership> UserMemberships { get; set; } = new List<UserOrganizationMembership>();

        /// <summary>
        /// Gets or sets the roles.
        /// </summary>
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}

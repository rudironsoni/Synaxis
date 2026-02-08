// <copyright file="UserOrganizationMembership.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity
{
    using Synaxis.InferenceGateway.Infrastructure.Data.Interfaces;

    /// <summary>
    /// Represents a user's membership in an organization.
    /// Implements soft delete to support cascade deletion when organization is soft deleted.
    /// </summary>
    /// <summary>
    /// UserOrganizationMembership class.
    /// </summary>
    public class UserOrganizationMembership : ISoftDeletable
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationId.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the organization role.
        /// </summary>
        public required string OrganizationRole { get; set; } = "Member";

        /// <summary>
        /// Gets or sets the PrimaryGroupId.
        /// </summary>
        public Guid? PrimaryGroupId { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in requests per minute.
        /// </summary>
        public int? RateLimitRpm { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in tokens per minute.
        /// </summary>
        public int? RateLimitTpm { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether auto-optimization is allowed.
        /// </summary>
        public bool AllowAutoOptimization { get; set; } = true;

        /// <summary>
        /// Gets or sets the membership status.
        /// </summary>
        public required string Status { get; set; } = "Active";

        /// <summary>
        /// Gets or sets the JoinedAt.
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the CreatedAt.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the UpdatedAt.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Soft delete properties

        /// <summary>
        /// Gets or sets the DeletedAt.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Gets or sets the DeletedBy.
        /// </summary>
        public Guid? DeletedBy { get; set; }

        // Navigation properties

        /// <summary>
        /// Gets or sets the User.
        /// </summary>
        public SynaxisUser User { get; set; } = null!;

        /// <summary>
        /// Gets or sets the Organization.
        /// </summary>
        public Organization Organization { get; set; } = null!;

        /// <summary>
        /// Gets or sets the PrimaryGroup.
        /// </summary>
        public Group? PrimaryGroup { get; set; }
    }
}

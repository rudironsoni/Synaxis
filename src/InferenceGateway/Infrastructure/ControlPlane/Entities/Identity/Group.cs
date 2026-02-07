// <copyright file="Group.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity
{
    /// <summary>
    /// Represents a group within an organization.
    /// </summary>
    public class Group
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationId.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        required public string Name { get; set; }

        /// <summary>
        /// Gets or sets the group description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the URL slug.
        /// </summary>
        required public string Slug { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in requests per minute.
        /// </summary>
        public int? RateLimitRpm { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in tokens per minute.
        /// </summary>
        public int? RateLimitTpm { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether automatic optimization is allowed.
        /// </summary>
        public bool AllowAutoOptimization { get; set; } = true;

        /// <summary>
        /// Gets or sets the group status.
        /// </summary>
        required public string Status { get; set; } = "Active";

        /// <summary>
        /// Gets or sets a value indicating whether this is the default group.
        /// </summary>
        public bool IsDefaultGroup { get; set; }

        /// <summary>
        /// Gets or sets the CreatedAt.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the CreatedBy.
        /// </summary>
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedAt.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy.
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the DeletedAt.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        // Navigation properties

        /// <summary>
        /// Gets or sets the Organization.
        /// </summary>
        public Organization Organization { get; set; } = null!;

        /// <summary>
        /// Gets or sets the user group memberships.
        /// </summary>
        public ICollection<UserGroupMembership> UserMemberships { get; set; } = new List<UserGroupMembership>();
    }
}

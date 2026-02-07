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
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        required public string Name { get; set; }

        public string? Description { get; set; }

        required public string Slug { get; set; }

        public int? RateLimitRpm { get; set; }

        public int? RateLimitTpm { get; set; }

        public bool AllowAutoOptimization { get; set; } = true;

        required public string Status { get; set; } = "Active";

        public bool IsDefaultGroup { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Organization Organization { get; set; } = null!;

        public ICollection<UserGroupMembership> UserMemberships { get; set; } = new List<UserGroupMembership>();
    }
}

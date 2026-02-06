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
    public class UserOrganizationMembership : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public required string OrganizationRole { get; set; } = "Member";
        public Guid? PrimaryGroupId { get; set; }
        public int? RateLimitRpm { get; set; }
        public int? RateLimitTpm { get; set; }
        public bool AllowAutoOptimization { get; set; } = true;
        public required string Status { get; set; } = "Active";
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Soft delete properties
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation properties
        public SynaxisUser User { get; set; } = null!;
        public Organization Organization { get; set; } = null!;
        public Group? PrimaryGroup { get; set; }
    }
}
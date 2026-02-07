// <copyright file="UserGroupMembership.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity
{
    /// <summary>
    /// Represents a user's membership in a group.
    /// </summary>
    public class UserGroupMembership
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid GroupId { get; set; }

        public required string GroupRole { get; set; } = "Member";

        public bool IsPrimary { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public SynaxisUser User { get; set; } = null!;

        public Group Group { get; set; } = null!;
    }
}

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
        /// <summary>
        /// Gets or sets the unique identifier for this membership record.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who is a member of the group.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the group that the user belongs to.
        /// </summary>
        public Guid GroupId { get; set; }

        /// <summary>
        /// Gets or sets the role of the user within the group (e.g., Member, Admin).
        /// </summary>
        public required string GroupRole { get; set; } = "Member";

        /// <summary>
        /// Gets or sets a value indicating whether this is the user's primary group membership.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the user joined the group.
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the user associated with this membership.
        /// </summary>
        public SynaxisUser User { get; set; } = null!;

        /// <summary>
        /// Gets or sets the group associated with this membership.
        /// </summary>
        public Group Group { get; set; } = null!;
    }
}

// <copyright file="SynaxisUser.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity
{
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// Represents a user in the system, extending ASP.NET Core Identity.
    /// </summary>
    public class SynaxisUser : IdentityUser<Guid>
    {
        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the user's current status (e.g., Active, Inactive).
        /// </summary>
        public required string Status { get; set; } = "Active";

        /// <summary>
        /// Gets or sets the timestamp when the user was soft-deleted, or null if not deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the timestamp when the user account was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the collection of organization memberships for this user.
        /// </summary>
        public ICollection<UserOrganizationMembership> OrganizationMemberships { get; set; } = new List<UserOrganizationMembership>();

        /// <summary>
        /// Gets or sets the collection of group memberships for this user.
        /// </summary>
        public ICollection<UserGroupMembership> GroupMemberships { get; set; } = new List<UserGroupMembership>();
    }
}

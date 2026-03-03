// <copyright file="UserRole.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity
{
    /// <summary>
    /// Represents a user-role assignment within an organization.
    /// </summary>
    public class UserRole
    {
        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the RoleId.
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationId.
        /// </summary>
        public Guid OrganizationId { get; set; }

        // Navigation properties

        /// <summary>
        /// Gets or sets the User.
        /// </summary>
        public SynaxisUser User { get; set; } = null!;

        /// <summary>
        /// Gets or sets the Role.
        /// </summary>
        public Role Role { get; set; } = null!;

        /// <summary>
        /// Gets or sets the Organization.
        /// </summary>
        public Organization Organization { get; set; } = null!;
    }
}

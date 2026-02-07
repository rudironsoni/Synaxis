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
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid OrganizationId { get; set; }

        // Navigation properties
        public SynaxisUser User { get; set; } = null!;
        public Role Role { get; set; } = null!;
        public Organization Organization { get; set; } = null!;
    }
}

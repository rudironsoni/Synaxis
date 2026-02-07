// <copyright file="TeamMembership.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Junction table for user-team relationships.
    /// </summary>
    public class TeamMembership
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the user navigation property.
        /// </summary>
        public virtual User User { get; set; }

        /// <summary>
        /// Gets or sets the team identifier.
        /// </summary>
        public Guid TeamId { get; set; }

        /// <summary>
        /// Gets or sets the team navigation property.
        /// </summary>
        public virtual Team Team { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the organization navigation property.
        /// </summary>
        public virtual Organization Organization { get; set; }

        /// <summary>
        /// Gets or sets the role: admin, member.
        /// </summary>
        [Required]
        public string Role { get; set; } = "member";

        /// <summary>
        /// Gets or sets the date when user joined the team.
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the identifier of the user who invited this member.
        /// </summary>
        public Guid? InvitedBy { get; set; }

        /// <summary>
        /// Gets or sets the inviter navigation property.
        /// </summary>
        public virtual User Inviter { get; set; }
    }
}

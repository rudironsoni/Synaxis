// <copyright file="Invitation.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Team invitation entity for managing team member invitations.
    /// </summary>
    public class Invitation
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the organization navigation property.
        /// </summary>
        public virtual Organization? Organization { get; set; }

        /// <summary>
        /// Gets or sets the team identifier.
        /// </summary>
        public Guid TeamId { get; set; }

        /// <summary>
        /// Gets or sets the team navigation property.
        /// </summary>
        public virtual Group? Team { get; set; }

        /// <summary>
        /// Gets or sets the email address of the invitee.
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the role to assign when invitation is accepted.
        /// </summary>
        [Required]
        public string Role { get; set; } = "member";

        /// <summary>
        /// Gets or sets the secure invitation token.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user who created the invitation.
        /// </summary>
        public Guid InvitedBy { get; set; }

        /// <summary>
        /// Gets or sets the inviter navigation property.
        /// </summary>
        public virtual SynaxisUser? Inviter { get; set; }

        /// <summary>
        /// Gets or sets the invitation status: pending, accepted, declined, cancelled, expired.
        /// </summary>
        [Required]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the acceptance timestamp.
        /// </summary>
        public DateTime? AcceptedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who accepted the invitation.
        /// </summary>
        public Guid? AcceptedBy { get; set; }

        /// <summary>
        /// Gets or sets the declination timestamp.
        /// </summary>
        public DateTime? DeclinedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who declined the invitation.
        /// </summary>
        public Guid? DeclinedBy { get; set; }

        /// <summary>
        /// Gets or sets the cancellation timestamp.
        /// </summary>
        public DateTime? CancelledAt { get; set; }

        /// <summary>
        /// Gets or sets the user who cancelled the invitation.
        /// </summary>
        public Guid? CancelledBy { get; set; }

        /// <summary>
        /// Gets a value indicating whether the invitation is expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > this.ExpiresAt;

        /// <summary>
        /// Gets a value indicating whether the invitation is still valid.
        /// </summary>
        public bool IsValid => string.Equals(this.Status, "pending", StringComparison.Ordinal) && !this.IsExpired;
    }
}

// <copyright file="CollectionMembership.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Junction table for user-collection relationships.
    /// </summary>
    public class CollectionMembership
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
        /// Gets or sets the collection identifier.
        /// </summary>
        public Guid CollectionId { get; set; }

        /// <summary>
        /// Gets or sets the collection navigation property.
        /// </summary>
        public virtual Collection Collection { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the organization navigation property.
        /// </summary>
        public virtual Organization Organization { get; set; }

        /// <summary>
        /// Gets or sets the role: admin, member, viewer.
        /// </summary>
        [Required]
        public string Role { get; set; } = "member";

        /// <summary>
        /// Gets or sets the date when user joined the collection.
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the identifier of the user who added this member.
        /// </summary>
        public Guid? AddedBy { get; set; }

        /// <summary>
        /// Gets or sets the adder navigation property.
        /// </summary>
        public virtual User Adder { get; set; }
    }
}

// <copyright file="Collection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Collection within an organization - a grouping of resources (models, prompts, etc.).
    /// </summary>
    public class Collection
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
        public virtual Organization Organization { get; set; }

        /// <summary>
        /// Gets or sets the team identifier (optional - collections can be org-wide or team-specific).
        /// </summary>
        public Guid? TeamId { get; set; }

        /// <summary>
        /// Gets or sets the team navigation property.
        /// </summary>
        public virtual Team Team { get; set; }

        /// <summary>
        /// Gets or sets the collection slug.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Slug { get; set; }

        /// <summary>
        /// Gets or sets the collection name.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the collection description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the collection is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the collection type (e.g., 'models', 'prompts', 'datasets').
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "general";

        /// <summary>
        /// Gets or sets the collection visibility (public, private, team).
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Visibility { get; set; } = "private";

        /// <summary>
        /// Gets or sets the tags for the collection.
        /// </summary>
        public IList<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the metadata for the collection.
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the identifier of the user who created the collection.
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the creator navigation property.
        /// </summary>
        public virtual User Creator { get; set; }

        /// <summary>
        /// Gets or sets the collection memberships navigation property.
        /// </summary>
        public virtual ICollection<CollectionMembership> CollectionMemberships { get; set; }
    }
}

// <copyright file="OrganizationApiKey.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Organization API key entity for managing API keys at the organization level.
    /// </summary>
    public class OrganizationApiKey
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
        /// Gets or sets the user who created the API key.
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the creator navigation property.
        /// </summary>
        public virtual User? Creator { get; set; }

        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key hash (SHA-256).
        /// </summary>
        [Required]
        [StringLength(64)]
        public string KeyHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key prefix (first 8 characters for display).
        /// </summary>
        [Required]
        [StringLength(8)]
        public string KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the permissions as JSON.
        /// </summary>
        public IDictionary<string, object> Permissions { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the expiration timestamp (null = never expires).
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the last used timestamp.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Gets or sets the revocation timestamp.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the revocation reason.
        /// </summary>
        [StringLength(500)]
        public string RevokedReason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the key is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the total number of requests made with this API key.
        /// </summary>
        public long? TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets the number of error requests made with this API key.
        /// </summary>
        public long? ErrorCount { get; set; }

        /// <summary>
        /// Gets a value indicating whether the API key is expired.
        /// </summary>
        public bool IsExpired => this.ExpiresAt.HasValue && DateTime.UtcNow > this.ExpiresAt.Value;

        /// <summary>
        /// Gets a value indicating whether the API key is revoked.
        /// </summary>
        public bool IsRevoked => this.RevokedAt.HasValue;

        /// <summary>
        /// Gets a value indicating whether the API key is valid (active, not expired, not revoked).
        /// </summary>
        public bool IsValid => this.IsActive && !this.IsExpired && !this.IsRevoked;
    }
}

// <copyright file="AuditLog.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Immutable audit log entry for compliance and security.
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// Gets or sets the unique identifier for the audit log entry.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        [Required]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event category.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string EventCategory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the action performed.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        [StringLength(100)]
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        [StringLength(255)]
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional event metadata as JSON.
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user agent string.
        /// </summary>
        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the region where the event occurred.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the cryptographic hash for tamper detection.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string IntegrityHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the hash of previous log entry for chain verification.
        /// </summary>
        [StringLength(128)]
        public string PreviousHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when event occurred (immutable).
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the full-text search vector (PostgreSQL tsvector).
        /// This is a computed column for efficient full-text search.
        /// </summary>
        public string? SearchVector { get; set; }

        /// <summary>
        /// Gets or sets the organization navigation property.
        /// </summary>
        public virtual Organization? Organization { get; set; }

        /// <summary>
        /// Gets or sets the user navigation property.
        /// </summary>
        public virtual User? User { get; set; }
    }
}

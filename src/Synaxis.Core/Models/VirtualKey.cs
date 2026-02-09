// <copyright file="VirtualKey.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// API Key with budget and rate limiting.
    /// </summary>
    public class VirtualKey
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the key hash.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string KeyHash { get; set; } = string.Empty;

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
        public virtual Team? Team { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who created this key.
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the creator navigation property.
        /// </summary>
        public virtual User? Creator { get; set; }

        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the key is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the key is revoked.
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Gets or sets the revocation timestamp.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the reason for revocation.
        /// </summary>
        public string RevokedReason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum budget for this key (NULL = inherit from team).
        /// </summary>
        public decimal? MaxBudget { get; set; }

        /// <summary>
        /// Gets or sets the current spend against budget.
        /// </summary>
        public decimal CurrentSpend { get; set; } = 0.00m;

        /// <summary>
        /// Gets or sets the requests per minute limit (NULL = inherit from team).
        /// </summary>
        public int? RpmLimit { get; set; }

        /// <summary>
        /// Gets or sets the tokens per minute limit (NULL = inherit from team).
        /// </summary>
        public int? TpmLimit { get; set; }

        /// <summary>
        /// Gets or sets the allowed models (NULL = all models allowed).
        /// </summary>
        public IList<string> AllowedModels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the blocked models.
        /// </summary>
        public IList<string> BlockedModels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the key expiration date.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with this key.
        /// </summary>
        public IList<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the metadata dictionary.
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the region for database partitioning.
        /// </summary>
        [Required]
        public string UserRegion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the requests made with this virtual key.
        /// </summary>
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

        /// <summary>
        /// Gets a value indicating whether the key is expired.
        /// </summary>
        public bool IsExpired => this.ExpiresAt.HasValue && this.ExpiresAt.Value < DateTime.UtcNow;

        /// <summary>
        /// Gets a value indicating whether the key has exceeded budget.
        /// </summary>
        public bool IsOverBudget => this.MaxBudget.HasValue && this.CurrentSpend >= this.MaxBudget.Value;

        /// <summary>
        /// Gets the remaining budget.
        /// </summary>
        public decimal? RemainingBudget => this.MaxBudget.HasValue ? this.MaxBudget.Value - this.CurrentSpend : null;
    }
}

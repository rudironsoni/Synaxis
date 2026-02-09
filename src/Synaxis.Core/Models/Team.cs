// <copyright file="Team.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Team within an organization.
    /// </summary>
    public class Team
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
        /// Gets or sets the team slug.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the team name.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the team description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the team is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the monthly budget for all team keys (NULL = no limit).
        /// </summary>
        public decimal? MonthlyBudget { get; set; }

        /// <summary>
        /// Gets or sets the alert threshold (percentage of budget).
        /// </summary>
        public decimal BudgetAlertThreshold { get; set; } = 80.00m;

        /// <summary>
        /// Gets or sets the allowed models (NULL = inherit from org).
        /// </summary>
        public IList<string> AllowedModels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the blocked models.
        /// </summary>
        public IList<string> BlockedModels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the team memberships navigation property.
        /// </summary>
        public virtual ICollection<TeamMembership> TeamMemberships { get; set; } = new List<TeamMembership>();

        /// <summary>
        /// Gets or sets the virtual keys navigation property.
        /// </summary>
        public virtual ICollection<VirtualKey> VirtualKeys { get; set; } = new List<VirtualKey>();
    }
}

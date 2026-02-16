// <copyright file="SubscriptionPlan.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Subscription plan template with limits configuration.
    /// </summary>
    public class SubscriptionPlan
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the plan slug.
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string Slug { get; set; }

        /// <summary>
        /// Gets or sets the plan name.
        /// </summary>
        [Required]
        [StringLength(255)]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the plan description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the plan is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the monthly price in USD.
        /// </summary>
        public decimal? MonthlyPriceUsd { get; set; }

        /// <summary>
        /// Gets or sets the yearly price in USD.
        /// </summary>
        public decimal? YearlyPriceUsd { get; set; }

        /// <summary>
        /// Gets or sets the limits configuration as JSON.
        /// </summary>
        public IDictionary<string, object> LimitsConfig { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the feature flags.
        /// </summary>
        public IDictionary<string, object> Features { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

// <copyright file="SpendLog.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Tracks spending and usage for billing.
    /// </summary>
    public class SpendLog
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
        /// Gets or sets the team identifier.
        /// </summary>
        public Guid? TeamId { get; set; }

        /// <summary>
        /// Gets or sets the team navigation property.
        /// </summary>
        public virtual Team Team { get; set; }

        /// <summary>
        /// Gets or sets the virtual key identifier.
        /// </summary>
        public Guid? VirtualKeyId { get; set; }

        /// <summary>
        /// Gets or sets the virtual key navigation property.
        /// </summary>
        public virtual VirtualKey VirtualKey { get; set; }

        /// <summary>
        /// Gets or sets the request identifier.
        /// </summary>
        public Guid? RequestId { get; set; }

        /// <summary>
        /// Gets or sets the amount in USD (base currency).
        /// </summary>
        public decimal AmountUsd { get; set; }

        /// <summary>
        /// Gets or sets the model used for the request.
        /// </summary>
        [StringLength(100)]
        public string Model { get; set; }

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        [StringLength(100)]
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens used.
        /// </summary>
        public int Tokens { get; set; }

        /// <summary>
        /// Gets or sets the region where the spend occurred.
        /// </summary>
        [StringLength(50)]
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

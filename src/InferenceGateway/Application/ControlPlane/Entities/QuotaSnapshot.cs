// <copyright file="QuotaSnapshot.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;

    /// <summary>
    /// Represents a snapshot of quota information.
    /// </summary>
    public sealed class QuotaSnapshot
    {
        /// <summary>
        /// Gets or sets the snapshot ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the account ID.
        /// </summary>
        public string AccountId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quota JSON.
        /// </summary>
        public string QuotaJson { get; set; } = "{}";

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

// <copyright file="DeviationEntry.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;

    /// <summary>
    /// Represents a deviation entry for API specification tracking.
    /// </summary>
    public sealed class DeviationEntry
    {
        /// <summary>
        /// Gets or sets the deviation entry ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the deviation reason.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the mitigation strategy.
        /// </summary>
        public string Mitigation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the deviation status.
        /// </summary>
        public DeviationStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the tenant navigation property.
        /// </summary>
        public Tenant? Tenant { get; set; }
    }
}

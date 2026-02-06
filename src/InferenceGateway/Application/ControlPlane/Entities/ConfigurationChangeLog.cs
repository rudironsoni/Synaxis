// <copyright file="ConfigurationChangeLog.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;

    /// <summary>
    /// Audit log for configuration changes.
    /// Provides full accountability for who changed what and when.
    /// </summary>
    public record ConfigurationChangeLog
    {
        /// <summary>
        /// Gets or initializes the change log ID.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or initializes the type of configuration changed (e.g., Provider, RoutingPolicy).
        /// </summary>
        public string ChangeType { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the entity identifier that was changed.
        /// </summary>
        public string EntityId { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the action performed: Create, Update, Delete.
        /// </summary>
        public string Action { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the JSON snapshot of previous state.
        /// </summary>
        public string? PreviousValue { get; init; }

        /// <summary>
        /// Gets or initializes the JSON snapshot of new state.
        /// </summary>
        public string? NewValue { get; init; }

        /// <summary>
        /// Gets or initializes the username of who made the change.
        /// </summary>
        public string ChangedBy { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the tenant ID context.
        /// </summary>
        public string? TenantId { get; init; }

        /// <summary>
        /// Gets or initializes the timestamp of change.
        /// </summary>
        public DateTime ChangedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or initializes the additional notes or reason.
        /// </summary>
        public string? Notes { get; init; }
    }
}

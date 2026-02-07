// <copyright file="AuditLog.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Audit
{
    /// <summary>
    /// Represents an audit log entry for tracking all system activities.
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// Gets or sets the audit log ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the action performed.
        /// </summary>
        required public string Action { get; set; }

        /// <summary>
        /// Gets or sets the entity type.
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// Gets or sets the entity ID.
        /// </summary>
        public string? EntityId { get; set; }

        /// <summary>
        /// Gets or sets the previous values.
        /// </summary>
        public string? PreviousValues { get; set; }

        /// <summary>
        /// Gets or sets the new values.
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the partition date.
        /// </summary>
        public DateTime PartitionDate { get; set; }

        // Navigation properties - will be configured with cross-schema relationships
    }
}

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
        public Guid Id { get; set; }

        public Guid? OrganizationId { get; set; }

        public Guid? UserId { get; set; }

        required public string Action { get; set; }

        public string? EntityType { get; set; }

        public string? EntityId { get; set; }

        public string? PreviousValues { get; set; }

        public string? NewValues { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public string? CorrelationId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime PartitionDate { get; set; }

        // Navigation properties - will be configured with cross-schema relationships
    }
}
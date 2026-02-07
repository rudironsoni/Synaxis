// <copyright file="ApiKey.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations
{
    /// <summary>
    /// Represents an API key for programmatic access.
    /// </summary>
    public class ApiKey
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public required string Name { get; set; }

        public required string KeyHash { get; set; }

        public required string KeyPrefix { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public string? Scopes { get; set; }

        public int? RateLimitRpm { get; set; }

        public int? RateLimitTpm { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastUsedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid? CreatedBy { get; set; }

        public DateTime? RevokedAt { get; set; }

        public Guid? RevokedBy { get; set; }

        public string? RevocationReason { get; set; }

        // Navigation properties - will be configured with cross-schema relationships
    }
}

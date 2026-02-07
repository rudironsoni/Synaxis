// <copyright file="ProviderHealthStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations
{
    /// <summary>
    /// Represents the health status of a provider for an organization.
    /// </summary>
    public class ProviderHealthStatus
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid OrganizationProviderId { get; set; }
        public bool IsHealthy { get; set; } = true;
        public decimal HealthScore { get; set; } = 1.0m;
        public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSuccessAt { get; set; }
        public DateTime? LastFailureAt { get; set; }
        public int ConsecutiveFailures { get; set; }
        public string? LastErrorMessage { get; set; }
        public string? LastErrorCode { get; set; }
        public int? AverageLatencyMs { get; set; }
        public decimal? SuccessRate { get; set; }
        public bool IsInCooldown { get; set; }
        public DateTime? CooldownUntil { get; set; }

        // Navigation properties
        public OrganizationProvider OrganizationProvider { get; set; } = null!;
    }
}

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
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationId.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationProviderId.
        /// </summary>
        public Guid OrganizationProviderId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the provider is healthy.
        /// </summary>
        public bool IsHealthy { get; set; } = true;

        /// <summary>
        /// Gets or sets the HealthScore.
        /// </summary>
        public decimal HealthScore { get; set; } = 1.0m;

        /// <summary>
        /// Gets or sets the LastCheckedAt.
        /// </summary>
        public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the LastSuccessAt.
        /// </summary>
        public DateTime? LastSuccessAt { get; set; }

        /// <summary>
        /// Gets or sets the LastFailureAt.
        /// </summary>
        public DateTime? LastFailureAt { get; set; }

        /// <summary>
        /// Gets or sets the ConsecutiveFailures.
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// Gets or sets the LastErrorMessage.
        /// </summary>
        public string? LastErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the LastErrorCode.
        /// </summary>
        public string? LastErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the AverageLatencyMs.
        /// </summary>
        public int? AverageLatencyMs { get; set; }

        /// <summary>
        /// Gets or sets the SuccessRate.
        /// </summary>
        public decimal? SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the provider is in cooldown.
        /// </summary>
        public bool IsInCooldown { get; set; }

        /// <summary>
        /// Gets or sets the CooldownUntil.
        /// </summary>
        public DateTime? CooldownUntil { get; set; }

        // Navigation properties

        /// <summary>
        /// Gets or sets the OrganizationProvider.
        /// </summary>
        public OrganizationProvider OrganizationProvider { get; set; } = null!;
    }
}

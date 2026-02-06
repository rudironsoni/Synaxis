// <copyright file="ProviderRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;
    using Synaxis.InferenceGateway.Application.Configuration;

    /// <summary>
    /// Represents a user's request to add a custom provider (BYOK).
    /// Requires administrator approval before activation.
    /// </summary>
    public record ProviderRequest
    {
        /// <summary>
        /// Gets or initializes the provider request ID.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or initializes the tenant ID.
        /// </summary>
        public string TenantId { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the user ID.
        /// </summary>
        public string UserId { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the proposed provider configuration.
        /// </summary>
        public ProviderConfig ProposedConfig { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the current approval workflow status.
        /// </summary>
        public ProviderRequestStatus Status { get; init; }

        /// <summary>
        /// Gets or initializes the administrator notes during review.
        /// </summary>
        public string? AdminNote { get; init; }

        /// <summary>
        /// Gets or initializes the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// Gets or initializes the review timestamp.
        /// </summary>
        public DateTime? ReviewedAt { get; init; }

        /// <summary>
        /// Gets or initializes the reviewer username.
        /// </summary>
        public string? ReviewedBy { get; init; }

        /// <summary>
        /// Gets or initializes the results from automated health check performed after approval.
        /// </summary>
        public HealthCheckResult? HealthCheckResult { get; init; }

        /// <summary>
        /// Gets or initializes the results from sandbox testing performed before activation.
        /// </summary>
        public SandboxTestResult? SandboxTestResult { get; init; }
    }
}

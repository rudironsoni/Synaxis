// <copyright file="TokenUsage.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;

    /// <summary>
    /// Represents token usage information for a request.
    /// </summary>
    public sealed class TokenUsage
    {
        /// <summary>
        /// Gets or sets the token usage ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the request ID.
        /// </summary>
        public string RequestId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the project ID.
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the number of input tokens.
        /// </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of output tokens.
        /// </summary>
        public int OutputTokens { get; set; }

        /// <summary>
        /// Gets or sets the estimated cost.
        /// </summary>
        public decimal CostEstimate { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the tenant navigation property.
        /// </summary>
        public Tenant? Tenant { get; set; }

        /// <summary>
        /// Gets or sets the project navigation property.
        /// </summary>
        public Project? Project { get; set; }

        /// <summary>
        /// Gets or sets the user navigation property.
        /// </summary>
        public User? User { get; set; }
    }
}

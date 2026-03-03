// <copyright file="RequestLog.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;

    /// <summary>
    /// Represents a request log entry.
    /// </summary>
    public sealed class RequestLog
    {
        /// <summary>
        /// Gets or sets the log entry ID.
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
        /// Gets or sets the endpoint.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Gets or sets the latency in milliseconds.
        /// </summary>
        public int? LatencyMs { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

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

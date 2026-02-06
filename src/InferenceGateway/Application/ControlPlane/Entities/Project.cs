// <copyright file="Project.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a project within a tenant.
    /// </summary>
    public sealed class Project
    {
        /// <summary>
        /// Gets or sets the project ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project status.
        /// </summary>
        public ProjectStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the tenant navigation property.
        /// </summary>
        public Tenant? Tenant { get; set; }

        /// <summary>
        /// Gets the API keys collection.
        /// </summary>
        public ICollection<ApiKey> ApiKeys { get; } = new List<ApiKey>();

        /// <summary>
        /// Gets the users collection.
        /// </summary>
        public ICollection<User> Users { get; } = new List<User>();
    }
}

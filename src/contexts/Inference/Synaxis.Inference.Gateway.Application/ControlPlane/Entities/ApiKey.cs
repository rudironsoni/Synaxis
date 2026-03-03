// <copyright file="ApiKey.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;

    /// <summary>
    /// Represents an API key for project authentication.
    /// </summary>
    public sealed class ApiKey
    {
        /// <summary>
        /// Gets or sets the API key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the project ID.
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the key hash.
        /// </summary>
        public string KeyHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key status.
        /// </summary>
        public ApiKeyStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the last used timestamp.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Gets a value indicating whether the API key is active.
        /// </summary>
        public bool IsActive => this.Status == ApiKeyStatus.Active;

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

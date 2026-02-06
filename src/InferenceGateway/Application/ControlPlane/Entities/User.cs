// <copyright file="User.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public sealed class User
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password hash.
        /// </summary>
        public string? PasswordHash { get; set; }

        /// <summary>
        /// Gets or sets the user role.
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// Gets or sets the authentication provider.
        /// </summary>
        public string AuthProvider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider user ID.
        /// </summary>
        public string ProviderUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the tenant navigation property.
        /// </summary>
        public Tenant? Tenant { get; set; }

        /// <summary>
        /// Gets the projects collection.
        /// </summary>
        public ICollection<Project> Projects { get; } = new List<Project>();
    }
}

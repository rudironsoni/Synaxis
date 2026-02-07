// <copyright file="Tenant.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a tenant in the multi-tenant system.
    /// </summary>
    public sealed class Tenant
    {
        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tenant region.
        /// </summary>
        public TenantRegion Region { get; set; }

        /// <summary>
        /// Gets or sets the tenant status.
        /// </summary>
        public TenantStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the BYOK key ID.
        /// </summary>
        public Guid? ByokKeyId { get; set; }

        /// <summary>
        /// Gets or sets the encrypted BYOK key.
        /// </summary>
        public byte[] EncryptedByokKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets the projects collection.
        /// </summary>
        public ICollection<Project> Projects { get; } = new List<Project>();

        /// <summary>
        /// Gets the users collection.
        /// </summary>
        public ICollection<User> Users { get; } = new List<User>();

        /// <summary>
        /// Gets the OAuth accounts collection.
        /// </summary>
        public ICollection<OAuthAccount> OAuthAccounts { get; } = new List<OAuthAccount>();
    }
}

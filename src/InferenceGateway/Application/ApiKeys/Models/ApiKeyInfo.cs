// <copyright file="ApiKeyInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ApiKeys.Models
{
    using System;

    /// <summary>
    /// Response model for listing API keys.
    /// </summary>
    public class ApiKeyInfo
    {
        /// <summary>
        /// Gets or sets the API key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the API key prefix.
        /// </summary>
        public required string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the scopes.
        /// </summary>
        public string[] Scopes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a value indicating whether the key is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the last used date.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the revocation date.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the revocation reason.
        /// </summary>
        public string? RevocationReason { get; set; }
    }
}

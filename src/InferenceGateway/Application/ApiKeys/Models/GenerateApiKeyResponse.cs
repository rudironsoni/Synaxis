// <copyright file="GenerateApiKeyResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ApiKeys.Models
{
    using System;

    /// <summary>
    /// Response model for API key generation.
    /// </summary>
    public class GenerateApiKeyResponse
    {
        /// <summary>
        /// Gets or sets the API key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the full API key (only returned once at creation).
        /// </summary>
        public required string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the API key prefix (visible for identification).
        /// </summary>
        public required string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the scopes.
        /// </summary>
        public string[] Scopes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}

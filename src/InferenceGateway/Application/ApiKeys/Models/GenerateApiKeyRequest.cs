// <copyright file="GenerateApiKeyRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ApiKeys.Models
{
    using System;

    /// <summary>
    /// Request model for generating a new API key.
    /// </summary>
    public class GenerateApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the scopes for the API key.
        /// </summary>
        public string[] Scopes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the expiration date (optional).
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in requests per minute (optional).
        /// </summary>
        public int? RateLimitRpm { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in tokens per minute (optional).
        /// </summary>
        public int? RateLimitTpm { get; set; }

        /// <summary>
        /// Gets or sets the user ID who created this API key (optional).
        /// </summary>
        public Guid? CreatedBy { get; set; }
    }
}

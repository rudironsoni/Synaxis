// <copyright file="ApiKeyValidationResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ApiKeys.Models
{
    using System;

    /// <summary>
    /// Response model for API key validation.
    /// </summary>
    public class ApiKeyValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the API key is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the organization ID if valid.
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the API key ID if valid.
        /// </summary>
        public Guid? ApiKeyId { get; set; }

        /// <summary>
        /// Gets or sets the scopes if valid.
        /// </summary>
        public string[] Scopes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the rate limit in requests per minute (optional).
        /// </summary>
        public int? RateLimitRpm { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in tokens per minute (optional).
        /// </summary>
        public int? RateLimitTpm { get; set; }

        /// <summary>
        /// Gets or sets the error message if invalid.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}

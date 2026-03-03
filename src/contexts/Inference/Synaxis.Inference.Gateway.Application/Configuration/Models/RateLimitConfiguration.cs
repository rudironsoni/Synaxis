// <copyright file="RateLimitConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Configuration.Models
{
    /// <summary>
    /// Represents rate limit configuration.
    /// </summary>
    public class RateLimitConfiguration
    {
        /// <summary>
        /// Gets or sets the requests per minute limit.
        /// </summary>
        public int? RequestsPerMinute { get; set; }

        /// <summary>
        /// Gets or sets the tokens per minute limit.
        /// </summary>
        public int? TokensPerMinute { get; set; }

        /// <summary>
        /// Gets or sets the source of the configuration.
        /// </summary>
        public required string Source { get; set; }
    }
}

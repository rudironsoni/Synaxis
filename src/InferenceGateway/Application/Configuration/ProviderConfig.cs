// <copyright file="ProviderConfig.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration for a provider.
    /// </summary>
    public class ProviderConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether this provider is enabled for routing requests.
        /// Disabled providers are excluded from all routing calculations.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the API key for authentication with the provider.
        /// Required for most providers except those using custom authentication methods.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the account ID for Cloudflare Workers AI authentication.
        /// Required for Cloudflare provider type.
        /// </summary>
        public string? AccountId { get; set; }

        /// <summary>
        /// Gets or sets the project ID for Antigravity provider authentication.
        /// Required for Antigravity provider type.
        /// </summary>
        public string? ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the file path to store authentication tokens for Antigravity provider.
        /// Optional - if not provided, tokens are stored in memory only.
        /// </summary>
        public string? AuthStoragePath { get; set; }

        /// <summary>
        /// Gets or sets the priority tier for this provider (0 = highest priority).
        /// Lower tier numbers are tried first during failover.
        /// Providers within the same tier are load-balanced.
        /// </summary>
        public int Tier { get; set; }

        /// <summary>
        /// Gets or sets the list of model IDs supported by this provider.
        /// Used for model routing and availability checks.
        /// </summary>
        public IList<string> Models { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the provider type identifier (e.g., "OpenAI", "Groq", "Cohere", "Cloudflare").
        /// Determines the client implementation and request format.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the custom API endpoint URL to override the default provider endpoint.
        /// Optional - if not provided, uses the provider's default endpoint.
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the fallback endpoint URL to use if the primary endpoint fails.
        /// Optional - provides redundancy for critical providers.
        /// </summary>
        public string? FallbackEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the rate limit for requests per minute (RPM).
        /// Used for intelligent routing to avoid hitting provider limits.
        /// Optional - if not set, no RPM limit is enforced.
        /// </summary>
        public int? RateLimitRPM { get; set; }

        /// <summary>
        /// Gets or sets the rate limit for tokens per minute (TPM).
        /// Used for intelligent routing to avoid hitting provider limits.
        /// Optional - if not set, no TPM limit is enforced.
        /// </summary>
        public int? RateLimitTPM { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this provider offers free tier access.
        /// Free providers are prioritized by default in Ultra Miser Mode.
        /// </summary>
        public bool IsFree { get; set; } = false;

        /// <summary>
        /// Gets or sets custom HTTP headers to send with each API request.
        /// Required by some providers like GitHub Models for authentication.
        /// </summary>
        public IDictionary<string, string>? CustomHeaders { get; set; }

        /// <summary>
        /// Gets or sets the quality score (1-10) for this provider.
        /// Higher scores indicate better model quality/response accuracy.
        /// Used in intelligent routing calculation.
        /// Default: 5 (average quality).
        /// </summary>
        public int QualityScore { get; set; } = 5;

        /// <summary>
        /// Gets or sets the estimated quota remaining as percentage (0-100).
        /// Used for intelligent routing to prefer providers with higher remaining quota.
        /// Default: 100 (full quota).
        /// Updated dynamically by health monitoring jobs.
        /// </summary>
        public int EstimatedQuotaRemaining { get; set; } = 100;

        /// <summary>
        /// Gets or sets the average latency in milliseconds from recent health checks.
        /// Used in intelligent routing to prefer faster providers.
        /// Updated dynamically by health monitoring jobs.
        /// </summary>
        public int? AverageLatencyMs { get; set; }
    }
}

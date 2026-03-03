// <copyright file="OrganizationProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations
{
    /// <summary>
    /// Represents an organization's provider configuration.
    /// </summary>
    public class OrganizationProvider
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationId.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the ProviderId.
        /// </summary>
        public Guid ProviderId { get; set; }

        /// <summary>
        /// Gets or sets the encrypted API key for this provider.
        /// </summary>
        public string? ApiKeyEncrypted { get; set; }

        /// <summary>
        /// Gets or sets the custom endpoint URL for this provider.
        /// </summary>
        public string? CustomEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the input cost per 1 million tokens.
        /// </summary>
        public decimal? InputCostPer1MTokens { get; set; }

        /// <summary>
        /// Gets or sets the output cost per 1 million tokens.
        /// </summary>
        public decimal? OutputCostPer1MTokens { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether streaming is supported.
        /// </summary>
        public bool SupportsStreaming { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether tools are supported.
        /// </summary>
        public bool SupportsTools { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether vision is supported.
        /// </summary>
        public bool SupportsVision { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this provider is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this is the default provider.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in requests per minute.
        /// </summary>
        public int? RateLimitRpm { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in tokens per minute.
        /// </summary>
        public int? RateLimitTpm { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether health checks are enabled.
        /// </summary>
        public bool HealthCheckEnabled { get; set; } = true;

        // Navigation properties - will be configured with cross-schema relationships
    }
}

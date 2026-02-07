// <copyright file="Provider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Platform
{
    /// <summary>
    /// Represents an AI provider in the platform schema (tenant-agnostic).
    /// </summary>
    public class Provider
    {
        /// <summary>
        /// Gets or sets the unique identifier for the provider.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the unique key for the provider (e.g., "openai", "anthropic").
        /// </summary>
        required public string Key { get; set; }

        /// <summary>
        /// Gets or sets the display name for the provider.
        /// </summary>
        required public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the type of the provider (e.g., "OpenAI", "Anthropic").
        /// </summary>
        required public string ProviderType { get; set; }

        /// <summary>
        /// Gets or sets the base endpoint URL for the provider's API.
        /// </summary>
        public string? BaseEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the default environment variable name for the provider's API key.
        /// </summary>
        public string? DefaultApiKeyEnvironmentVariable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the provider supports streaming responses.
        /// </summary>
        public bool SupportsStreaming { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the provider supports tool/function calling.
        /// </summary>
        public bool SupportsTools { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the provider supports vision/image inputs.
        /// </summary>
        public bool SupportsVision { get; set; }

        /// <summary>
        /// Gets or sets the default input cost per 1 million tokens.
        /// </summary>
        public decimal? DefaultInputCostPer1MTokens { get; set; }

        /// <summary>
        /// Gets or sets the default output cost per 1 million tokens.
        /// </summary>
        public decimal? DefaultOutputCostPer1MTokens { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the provider is a free tier provider.
        /// </summary>
        public bool IsFreeTier { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the provider is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the provider is publicly available.
        /// </summary>
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Gets or sets the collection of models associated with this provider.
        /// </summary>
        public ICollection<Model> Models { get; set; } = new List<Model>();
    }
}

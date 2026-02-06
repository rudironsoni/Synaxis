// <copyright file="ProviderModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Represents a provider-specific model configuration.
    /// </summary>
    public sealed class ProviderModel
    {
        /// <summary>
        /// Gets or sets the provider model ID.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the provider ID (e.g. "nvidia").
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the global model ID (FK to GlobalModel).
        /// </summary>
        public string GlobalModelId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider-specific model ID sent to the provider API.
        /// </summary>
        public string ProviderSpecificId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the model is available.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Gets or sets the override input price.
        /// </summary>
        public decimal? OverrideInputPrice { get; set; }

        /// <summary>
        /// Gets or sets the override output price.
        /// </summary>
        public decimal? OverrideOutputPrice { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in requests per minute.
        /// </summary>
        public int? RateLimitRPM { get; set; }

        /// <summary>
        /// Gets or sets the rate limit in tokens per minute.
        /// </summary>
        public int? RateLimitTPM { get; set; }

        /// <summary>
        /// Gets or sets the global model navigation property.
        /// </summary>
        [ForeignKey("GlobalModelId")]
        public GlobalModel? GlobalModel { get; set; }
    }
}

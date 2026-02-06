// <copyright file="GlobalModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a global model definition.
    /// </summary>
    public sealed class GlobalModel
    {
        /// <summary>
        /// Gets or sets the canonical model ID (e.g. "llama-3.3-70b").
        /// </summary>
        [Key]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model family.
        /// </summary>
        public string Family { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the context window size.
        /// </summary>
        public int ContextWindow { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum output tokens.
        /// </summary>
        public int MaxOutputTokens { get; set; } = 0;

        /// <summary>
        /// Gets or sets the input price.
        /// </summary>
        public decimal InputPrice { get; set; }

        /// <summary>
        /// Gets or sets the output price.
        /// </summary>
        public decimal OutputPrice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model has open weights.
        /// </summary>
        public bool IsOpenWeights { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports tools.
        /// </summary>
        public bool SupportsTools { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports reasoning.
        /// </summary>
        public bool SupportsReasoning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports vision.
        /// </summary>
        public bool SupportsVision { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports audio.
        /// </summary>
        public bool SupportsAudio { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports structured output.
        /// </summary>
        public bool SupportsStructuredOutput { get; set; }

        /// <summary>
        /// Gets or sets the provider models navigation property.
        /// </summary>
        public IList<ProviderModel> ProviderModels { get; set; } = new List<ProviderModel>();
    }
}

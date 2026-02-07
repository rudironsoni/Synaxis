// <copyright file="Model.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Platform
{
    /// <summary>
    /// Represents an AI model in the platform schema (tenant-agnostic).
    /// </summary>
    public class Model
    {
        /// <summary>
        /// Gets or sets the unique identifier for the model.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the provider that owns this model.
        /// </summary>
        public Guid ProviderId { get; set; }

        /// <summary>
        /// Gets or sets the canonical identifier for the model (e.g., "gpt-4", "claude-3-opus").
        /// </summary>
        required public string CanonicalId { get; set; }

        /// <summary>
        /// Gets or sets the display name for the model.
        /// </summary>
        required public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description of the model.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the context window size in tokens.
        /// </summary>
        public int? ContextWindowTokens { get; set; }

        /// <summary>
        /// Gets or sets the maximum output tokens the model can generate.
        /// </summary>
        public int? MaxOutputTokens { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports streaming responses.
        /// </summary>
        public bool SupportsStreaming { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports tool/function calling.
        /// </summary>
        public bool SupportsTools { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports vision/image inputs.
        /// </summary>
        public bool SupportsVision { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the model is publicly available.
        /// </summary>
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Gets or sets the provider that owns this model.
        /// </summary>
        public Provider Provider { get; set; } = null!;
    }
}

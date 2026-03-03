// <copyright file="ModelCost.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Represents cost information for a model.
    /// </summary>
    public sealed class ModelCost
    {
        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the cost per token.
        /// </summary>
        public decimal CostPerToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a free tier model.
        /// </summary>
        public bool FreeTier { get; set; }
    }
}

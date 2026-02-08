// <copyright file="CostConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Configuration.Models
{
    /// <summary>
    /// Represents cost configuration for a model.
    /// </summary>
    public class CostConfiguration
    {
        /// <summary>
        /// Gets or sets the input cost per 1M tokens.
        /// </summary>
        public decimal InputCostPer1MTokens { get; set; }

        /// <summary>
        /// Gets or sets the output cost per 1M tokens.
        /// </summary>
        public decimal OutputCostPer1MTokens { get; set; }

        /// <summary>
        /// Gets or sets the source of the configuration.
        /// </summary>
        public required string Source { get; set; }
    }
}

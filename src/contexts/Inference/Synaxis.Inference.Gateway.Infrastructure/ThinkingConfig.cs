// <copyright file="ThinkingConfig.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Configuration for extended thinking capabilities in the Antigravity API.
    /// </summary>
    internal class ThinkingConfig
    {
        /// <summary>
        /// Gets or sets the thinking budget.
        /// </summary>
        [JsonPropertyName("thinkingBudget")]
        public int ThinkingBudget { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include thoughts.
        /// </summary>
        [JsonPropertyName("includeThoughts")]
        public bool IncludeThoughts { get; set; }
    }
}
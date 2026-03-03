// <copyright file="GenerationConfig.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Configuration for text generation in the Antigravity API.
    /// </summary>
    internal class GenerationConfig
    {
        /// <summary>
        /// Gets or sets the maximum number of output tokens.
        /// </summary>
        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }

        /// <summary>
        /// Gets or sets the temperature for generation.
        /// </summary>
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        /// <summary>
        /// Gets or sets the top-p value for nucleus sampling.
        /// </summary>
        [JsonPropertyName("topP")]
        public float TopP { get; set; }

        /// <summary>
        /// Gets or sets the top-k value for sampling.
        /// </summary>
        [JsonPropertyName("topK")]
        public int TopK { get; set; }

        /// <summary>
        /// Gets or sets the stop sequences.
        /// </summary>
        [JsonPropertyName("stopSequences")]
        public IList<string>? StopSequences { get; set; }

        /// <summary>
        /// Gets or sets the ThinkingConfig.
        /// </summary>
        [JsonPropertyName("thinkingConfig")]
        public ThinkingConfig? ThinkingConfig { get; set; }
    }
}
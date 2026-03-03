// <copyright file="RequestPayload.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the request payload for the Antigravity API.
    /// </summary>
    internal class RequestPayload
    {
        /// <summary>
        /// Gets or sets the contents of the request.
        /// </summary>
        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; } = new();

        /// <summary>
        /// Gets or sets the SystemInstruction.
        /// </summary>
        [JsonPropertyName("systemInstruction")]
        public SystemInstruction? SystemInstruction { get; set; }

        /// <summary>
        /// Gets or sets the GenerationConfig.
        /// </summary>
        [JsonPropertyName("generationConfig")]
        public GenerationConfig GenerationConfig { get; set; } = new();
    }
}
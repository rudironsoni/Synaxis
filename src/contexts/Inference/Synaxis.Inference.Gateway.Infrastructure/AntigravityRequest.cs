// <copyright file="AntigravityRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a request to the Antigravity API.
    /// </summary>
    internal class AntigravityRequest
    {
        /// <summary>
        /// Gets or sets the project identifier.
        /// </summary>
        [JsonPropertyName("project")]
        public string Project { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the RequestPayload.
        /// </summary>
        [JsonPropertyName("request")]
        public RequestPayload RequestPayload { get; set; } = new();
    }
}
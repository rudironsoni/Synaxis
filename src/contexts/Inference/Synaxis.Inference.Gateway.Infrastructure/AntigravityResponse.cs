// <copyright file="AntigravityResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a response from the Antigravity API.
    /// </summary>
    internal class AntigravityResponse
    {
        /// <summary>
        /// Gets or sets the candidates.
        /// </summary>
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; } = new();

        /// <summary>
        /// Gets or sets the model version.
        /// </summary>
        [JsonPropertyName("modelVersion")]
        public string ModelVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the response ID.
        /// </summary>
        [JsonPropertyName("responseId")]
        public string ResponseId { get; set; } = string.Empty;
    }
}
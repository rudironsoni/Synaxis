// <copyright file="Candidate.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a candidate response from the Antigravity API.
    /// </summary>
    internal class Candidate
    {
        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        [JsonPropertyName("content")]
        public Content? Content { get; set; }

        /// <summary>
        /// Gets or sets the finish reason.
        /// </summary>
        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }
}
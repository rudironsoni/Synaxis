// <copyright file="Part.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a part of content in the Antigravity API format.
    /// </summary>
    internal class Part
    {
        /// <summary>
        /// Gets or sets the text content.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
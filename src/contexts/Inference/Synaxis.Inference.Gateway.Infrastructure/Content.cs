// <copyright file="Content.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents content in the Antigravity API format.
    /// </summary>
    internal class Content
    {
        /// <summary>
        /// Gets or sets the role of the content.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        /// <summary>
        /// Gets or sets the parts of the content.
        /// </summary>
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new();
    }
}
// <copyright file="AntigravityResponseWrapper.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Wrapper for the Antigravity API response format.
    /// </summary>
    internal class AntigravityResponseWrapper
    {
        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        [JsonPropertyName("response")]
        public AntigravityResponse? Response { get; set; }
    }
}
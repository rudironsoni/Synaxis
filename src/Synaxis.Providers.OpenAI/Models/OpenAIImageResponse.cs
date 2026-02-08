// <copyright file="OpenAIImageResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an OpenAI image generation response.
    /// </summary>
    internal sealed class OpenAIImageResponse
    {
        /// <summary>
        /// Gets or initializes the created timestamp.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; init; }

        /// <summary>
        /// Gets or initializes the data.
        /// </summary>
        [JsonPropertyName("data")]
        public List<OpenAIImageData> Data { get; init; } = new List<OpenAIImageData>();
    }
}

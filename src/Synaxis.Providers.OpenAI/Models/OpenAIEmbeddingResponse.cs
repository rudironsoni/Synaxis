// <copyright file="OpenAIEmbeddingResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an OpenAI embedding response.
    /// </summary>
    internal sealed class OpenAIEmbeddingResponse
    {
        /// <summary>
        /// Gets or initializes the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the data.
        /// </summary>
        [JsonPropertyName("data")]
        public List<OpenAIEmbeddingData> Data { get; init; } = new List<OpenAIEmbeddingData>();

        /// <summary>
        /// Gets or initializes the usage.
        /// </summary>
        [JsonPropertyName("usage")]
        public OpenAIEmbeddingUsage? Usage { get; init; }
    }
}

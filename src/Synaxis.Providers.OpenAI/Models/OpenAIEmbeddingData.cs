// <copyright file="OpenAIEmbeddingData.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents embedding data in an OpenAI response.
    /// </summary>
    internal sealed class OpenAIEmbeddingData
    {
        /// <summary>
        /// Gets or initializes the index.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; init; }

        /// <summary>
        /// Gets or initializes the embedding vector.
        /// </summary>
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; init; } = Array.Empty<float>();

        /// <summary>
        /// Gets or initializes the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; init; } = string.Empty;
    }
}

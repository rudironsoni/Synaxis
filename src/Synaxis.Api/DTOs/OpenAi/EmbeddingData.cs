// <copyright file="EmbeddingData.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents embedding data.
    /// </summary>
    public sealed class EmbeddingData
    {
        /// <summary>
        /// Gets or sets the index of the embedding.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "embedding";

        /// <summary>
        /// Gets or sets the embedding vector.
        /// </summary>
        [JsonPropertyName("embedding")]
        public IList<float> Embedding { get; set; } = new List<float>();
    }
}

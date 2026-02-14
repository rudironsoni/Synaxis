// <copyright file="EmbeddingResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an embedding response compatible with OpenAI API.
    /// </summary>
    public sealed class EmbeddingResponse
    {
        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "list";

        /// <summary>
        /// Gets or sets the list of embedding data.
        /// </summary>
        [JsonPropertyName("data")]
        public IList<EmbeddingData> Data { get; set; } = new List<EmbeddingData>();

        /// <summary>
        /// Gets or sets the model used for the embedding.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the usage statistics.
        /// </summary>
        [JsonPropertyName("usage")]
        public Usage Usage { get; set; } = null!;
    }
}

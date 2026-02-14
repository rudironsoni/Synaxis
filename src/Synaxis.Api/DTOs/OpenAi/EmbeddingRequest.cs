// <copyright file="EmbeddingRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an embedding request compatible with OpenAI API.
    /// </summary>
    public sealed class EmbeddingRequest
    {
        /// <summary>
        /// Gets or sets the ID of the model to use.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the input text(s) to generate embeddings for.
        /// </summary>
        [JsonPropertyName("input")]
        public object Input { get; set; } = null!;

        /// <summary>
        /// Gets or sets the encoding format.
        /// </summary>
        [JsonPropertyName("encoding_format")]
        public string EncodingFormat { get; set; } = null!;

        /// <summary>
        /// Gets or sets the dimensions for the embedding.
        /// </summary>
        [JsonPropertyName("dimensions")]
        public int? Dimensions { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        [JsonPropertyName("user")]
        public string User { get; set; } = null!;
    }
}

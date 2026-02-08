// <copyright file="OpenAIEmbeddingRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an OpenAI embedding request.
    /// </summary>
    internal sealed class OpenAIEmbeddingRequest
    {
        /// <summary>
        /// Gets or initializes the model.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the input texts.
        /// </summary>
        [JsonPropertyName("input")]
        public List<string> Input { get; init; } = new List<string>();
    }
}

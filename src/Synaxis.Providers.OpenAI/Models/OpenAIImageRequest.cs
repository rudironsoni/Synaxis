// <copyright file="OpenAIImageRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an OpenAI image generation request.
    /// </summary>
    internal sealed class OpenAIImageRequest
    {
        /// <summary>
        /// Gets or initializes the prompt.
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the model.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the number of images.
        /// </summary>
        [JsonPropertyName("n")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? N { get; init; }

        /// <summary>
        /// Gets or initializes the size.
        /// </summary>
        [JsonPropertyName("size")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Size { get; init; }
    }
}

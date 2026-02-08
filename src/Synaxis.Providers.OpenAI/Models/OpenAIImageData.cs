// <copyright file="OpenAIImageData.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents image data in an OpenAI response.
    /// </summary>
    internal sealed class OpenAIImageData
    {
        /// <summary>
        /// Gets or initializes the URL.
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; init; }

        /// <summary>
        /// Gets or initializes the base64 JSON.
        /// </summary>
        [JsonPropertyName("b64_json")]
        public string? B64Json { get; init; }

        /// <summary>
        /// Gets or initializes the revised prompt.
        /// </summary>
        [JsonPropertyName("revised_prompt")]
        public string? RevisedPrompt { get; init; }
    }
}

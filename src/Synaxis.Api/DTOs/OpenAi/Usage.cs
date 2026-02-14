// <copyright file="Usage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents usage statistics for API requests compatible with OpenAI API.
    /// </summary>
    public sealed class Usage
    {
        /// <summary>
        /// Gets or sets the number of prompt tokens used.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of completion tokens used.
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Gets or sets the total number of tokens used.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of prompt tokens in the cached requests.
        /// </summary>
        [JsonPropertyName("prompt_tokens_details")]
        public PromptTokensDetails PromptTokensDetails { get; set; } = null!;

        /// <summary>
        /// Gets or sets the number of completion tokens in the cached requests.
        /// </summary>
        [JsonPropertyName("completion_tokens_details")]
        public CompletionTokensDetails CompletionTokensDetails { get; set; } = null!;
    }
}

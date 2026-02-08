// <copyright file="OpenAIEmbeddingUsage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents embedding usage in an OpenAI response.
    /// </summary>
    internal sealed class OpenAIEmbeddingUsage
    {
        /// <summary>
        /// Gets or initializes the prompt tokens.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; init; }

        /// <summary>
        /// Gets or initializes the total tokens.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; init; }
    }
}

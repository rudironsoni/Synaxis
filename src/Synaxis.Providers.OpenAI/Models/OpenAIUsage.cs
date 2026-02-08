// <copyright file="OpenAIUsage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents usage information in an OpenAI response.
    /// </summary>
    internal sealed class OpenAIUsage
    {
        /// <summary>
        /// Gets or initializes the prompt tokens.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; init; }

        /// <summary>
        /// Gets or initializes the completion tokens.
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; init; }

        /// <summary>
        /// Gets or initializes the total tokens.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; init; }
    }
}

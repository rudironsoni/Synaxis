// <copyright file="OpenAIChoice.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a choice in an OpenAI response.
    /// </summary>
    internal sealed class OpenAIChoice
    {
        /// <summary>
        /// Gets or initializes the index.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; init; }

        /// <summary>
        /// Gets or initializes the message.
        /// </summary>
        [JsonPropertyName("message")]
        public OpenAIMessage? Message { get; init; }

        /// <summary>
        /// Gets or initializes the finish reason.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; init; }
    }
}

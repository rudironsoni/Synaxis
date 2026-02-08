// <copyright file="OpenAIChatRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an OpenAI chat completion request.
    /// </summary>
    internal sealed class OpenAIChatRequest
    {
        /// <summary>
        /// Gets or initializes the model to use.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the messages array.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<OpenAIMessage> Messages { get; init; } = new List<OpenAIMessage>();

        /// <summary>
        /// Gets or initializes the temperature.
        /// </summary>
        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; init; }

        /// <summary>
        /// Gets or initializes the maximum tokens.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; init; }

        /// <summary>
        /// Gets a value indicating whether to stream the response.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; init; }
    }
}

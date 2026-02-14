// <copyright file="ChatCompletionRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a chat completion request compatible with OpenAI API.
    /// </summary>
    public sealed class ChatCompletionRequest
    {
        /// <summary>
        /// Gets or sets the ID of the model to use.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the messages to generate chat completions for.
        /// </summary>
        [JsonPropertyName("messages")]
        public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// Gets or sets the sampling temperature to use.
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets the nucleus sampling threshold.
        /// </summary>
        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }

        /// <summary>
        /// Gets or sets the number of chat completion choices to generate.
        /// </summary>
        [JsonPropertyName("n")]
        public int? N { get; set; }

        /// <summary>
        /// Gets or sets whether to stream back partial progress.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }

        /// <summary>
        /// Gets or sets the stop sequences.
        /// </summary>
        [JsonPropertyName("stop")]
        public object Stop { get; set; } = null!;

        /// <summary>
        /// Gets or sets the presence penalty.
        /// </summary>
        [JsonPropertyName("presence_penalty")]
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// Gets or sets the frequency penalty.
        /// </summary>
        [JsonPropertyName("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }

        /// <summary>
        /// Gets or sets the logit bias.
        /// </summary>
        [JsonPropertyName("logit_bias")]
        public IDictionary<string, int> LogitBias { get; set; } = null!;

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        [JsonPropertyName("user")]
        public string User { get; set; } = null!;
    }
}

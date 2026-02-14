// <copyright file="CompletionRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a text completion request compatible with OpenAI API (legacy).
    /// </summary>
    public sealed class CompletionRequest
    {
        /// <summary>
        /// Gets or sets the ID of the model to use.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the prompt to generate completions for.
        /// </summary>
        [JsonPropertyName("prompt")]
        public object Prompt { get; set; } = null!;

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
        /// Gets or sets the number of completions to generate.
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

        /// <summary>
        /// Gets or sets the echo flag.
        /// </summary>
        [JsonPropertyName("echo")]
        public bool? Echo { get; set; }

        /// <summary>
        /// Gets or sets the suffix to append after completion.
        /// </summary>
        [JsonPropertyName("suffix")]
        public string Suffix { get; set; } = null!;
    }
}

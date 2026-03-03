// <copyright file="OpenAIRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an OpenAI chat completion request.
    /// </summary>
    public class OpenAIRequest
    {
        /// <summary>
        /// Gets or sets the model identifier to use for the request.
        /// </summary>
        [JsonPropertyName("model")]
        [Required(ErrorMessage = "Model is required")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of messages in the conversation.
        /// </summary>
        [JsonPropertyName("messages")]
        [Required(ErrorMessage = "Messages are required")]
        [MinLength(1, ErrorMessage = "At least one message is required")]
        public IList<OpenAIMessage> Messages { get; set; } = new List<OpenAIMessage>();

        /// <summary>
        /// Gets or sets the list of tools available for the model to call.
        /// </summary>
        [JsonPropertyName("tools")]
        public IList<OpenAITool>? Tools { get; set; }

        /// <summary>
        /// Gets or sets the tool choice configuration.
        /// </summary>
        [JsonPropertyName("tool_choice")]
        public object? ToolChoice { get; set; }

        /// <summary>
        /// Gets or sets the response format configuration.
        /// </summary>
        [JsonPropertyName("response_format")]
        public object? ResponseFormat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to stream the response.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        /// <summary>
        /// Gets or sets the sampling temperature between 0 and 2.
        /// </summary>
        [JsonPropertyName("temperature")]
        [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0 and 2")]
        public double? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the nucleus sampling parameter between 0 and 1.
        /// </summary>
        [JsonPropertyName("top_p")]
        [Range(0.0, 1.0, ErrorMessage = "TopP must be between 0 and 1")]
        public double? TopP { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        [Range(1, int.MaxValue, ErrorMessage = "MaxTokens must be a positive integer")]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets the stop sequences where the model will stop generating.
        /// </summary>
        [JsonPropertyName("stop")]
        public object? Stop { get; set; }
    }
}

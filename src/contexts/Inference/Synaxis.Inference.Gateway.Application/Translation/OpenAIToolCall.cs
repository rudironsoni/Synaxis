// <copyright file="OpenAIToolCall.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a tool call made by the assistant.
    /// </summary>
    public class OpenAIToolCall
    {
        /// <summary>
        /// Gets or sets the unique identifier for the tool call.
        /// </summary>
        [JsonPropertyName("id")]
        [Required(ErrorMessage = "Tool call ID is required")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the tool call.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        /// <summary>
        /// Gets or sets the function call details.
        /// </summary>
        [JsonPropertyName("function")]
        public OpenAIFunctionCall? Function { get; set; }
    }
}

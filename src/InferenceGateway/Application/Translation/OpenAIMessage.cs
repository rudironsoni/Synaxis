// <copyright file="OpenAIMessage.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a message in an OpenAI chat conversation.
    /// </summary>
    public class OpenAIMessage
    {
        /// <summary>
        /// Gets or sets the role of the message author.
        /// </summary>
        [JsonPropertyName("role")]
        [Required(ErrorMessage = "Message role is required")]
        [RegularExpression("^(system|user|assistant|tool)$", ErrorMessage = "Role must be one of: system, user, assistant, tool")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        [JsonPropertyName("content")]
        public object? Content { get; set; }

        /// <summary>
        /// Gets or sets the name of the message author.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the tool calls made by the assistant.
        /// </summary>
        [JsonPropertyName("tool_calls")]
        public IList<OpenAIToolCall>? ToolCalls { get; set; }

        /// <summary>
        /// Gets or sets the tool call ID for tool response messages.
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; set; }
    }
}

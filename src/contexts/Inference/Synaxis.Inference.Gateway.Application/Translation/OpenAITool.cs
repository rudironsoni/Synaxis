// <copyright file="OpenAITool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a tool definition in OpenAI format.
    /// </summary>
    public class OpenAITool
    {
        /// <summary>
        /// Gets or sets the type of the tool.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        /// <summary>
        /// Gets or sets the function definition.
        /// </summary>
        [JsonPropertyName("function")]
        public OpenAIFunction? Function { get; set; }
    }
}

// <copyright file="OpenAIFunctionCall.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a function call with its arguments.
    /// </summary>
    public class OpenAIFunctionCall
    {
        /// <summary>
        /// Gets or sets the name of the function being called.
        /// </summary>
        [JsonPropertyName("name")]
        [Required(ErrorMessage = "Function name is required")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON string of function arguments.
        /// </summary>
        [JsonPropertyName("arguments")]
        [Required(ErrorMessage = "Function arguments are required")]
        public string Arguments { get; set; } = string.Empty;
    }
}

// <copyright file="OpenAIFunction.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a function definition in OpenAI format.
    /// </summary>
    public class OpenAIFunction
    {
        /// <summary>
        /// Gets or sets the name of the function.
        /// </summary>
        [JsonPropertyName("name")]
        [Required(ErrorMessage = "Function name is required")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the function.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the parameters schema for the function.
        /// </summary>
        [JsonPropertyName("parameters")]
        public object? Parameters { get; set; }
    }
}

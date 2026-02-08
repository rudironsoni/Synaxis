// <copyright file="OpenAIChatResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an OpenAI chat completion response.
    /// </summary>
    internal sealed class OpenAIChatResponse
    {
        /// <summary>
        /// Gets or initializes the ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the created timestamp.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; init; }

        /// <summary>
        /// Gets or initializes the model.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<OpenAIChoice> Choices { get; init; } = new List<OpenAIChoice>();

        /// <summary>
        /// Gets or initializes the usage.
        /// </summary>
        [JsonPropertyName("usage")]
        public OpenAIUsage? Usage { get; init; }
    }
}

// <copyright file="CompletionResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a text completion response compatible with OpenAI API (legacy).
    /// </summary>
    public sealed class CompletionResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "text_completion";

        /// <summary>
        /// Gets or sets the Unix timestamp when the completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the model used for the completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of completion choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public IList<CompletionChoice> Choices { get; set; } = new List<CompletionChoice>();

        /// <summary>
        /// Gets or sets the usage statistics.
        /// </summary>
        [JsonPropertyName("usage")]
        public Usage Usage { get; set; } = null!;
    }
}

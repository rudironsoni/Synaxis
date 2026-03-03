// <copyright file="OpenAiModelDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.OpenAi.Dto
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Data transfer object representing a model from OpenAI-compatible APIs.
    /// </summary>
    public sealed class OpenAiModelDto
    {
        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the object type (typically "model").
        /// </summary>
        [JsonPropertyName("object")]
        public string? Object { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp when the model was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long? Created { get; set; }

        /// <summary>
        /// Gets or sets the organization that owns the model.
        /// </summary>
        [JsonPropertyName("owned_by")]
        public string? OwnedBy { get; set; }
    }
}

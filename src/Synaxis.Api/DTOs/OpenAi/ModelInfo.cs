// <copyright file="ModelInfo.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents model information compatible with OpenAI API.
    /// </summary>
    public sealed class ModelInfo
    {
        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "model";

        /// <summary>
        /// Gets or sets the Unix timestamp when the model was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the model owner.
        /// </summary>
        [JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; } = "synaxis";
    }
}

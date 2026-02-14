// <copyright file="ModelList.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a list of available models compatible with OpenAI API.
    /// </summary>
    public sealed class ModelList
    {
        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "list";

        /// <summary>
        /// Gets or sets the list of available models.
        /// </summary>
        [JsonPropertyName("data")]
        public IList<ModelInfo> Data { get; set; } = new List<ModelInfo>();
    }
}

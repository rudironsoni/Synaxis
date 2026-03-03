// <copyright file="ProviderDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Data transfer object representing a model provider from models.dev.
    /// </summary>
    public sealed class ProviderDto
    {
        /// <summary>
        /// Gets or sets the dictionary of models keyed by model identifier.
        /// </summary>
        [JsonPropertyName("models")]
        public IDictionary<string, ModelDto>? Models { get; set; }
    }
}

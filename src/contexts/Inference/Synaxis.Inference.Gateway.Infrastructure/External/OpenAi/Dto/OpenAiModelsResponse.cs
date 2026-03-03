// <copyright file="OpenAiModelsResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.OpenAi.Dto
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Response object containing a list of models from OpenAI-compatible APIs.
    /// </summary>
    public sealed class OpenAiModelsResponse
    {
        /// <summary>
        /// Gets or sets the list of model data transfer objects.
        /// </summary>
        [JsonPropertyName("data")]
        public IList<OpenAiModelDto> Data { get; set; } = new List<OpenAiModelDto>();
    }
}

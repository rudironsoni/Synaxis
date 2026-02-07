// <copyright file="OpenAiModelsResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.OpenAi.Dto
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public sealed class OpenAiModelsResponse
    {
        [JsonPropertyName("data")]
        public List<OpenAiModelDto> Data { get; set; } = new();
    }
}

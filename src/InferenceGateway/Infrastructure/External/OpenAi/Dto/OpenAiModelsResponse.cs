using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Synaxis.InferenceGateway.Infrastructure.External.OpenAi.Dto
{
    public sealed class OpenAiModelsResponse
    {
        [JsonPropertyName("data")]
        public List<OpenAiModelDto> Data { get; set; } = new();
    }
}

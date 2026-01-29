using System.Text.Json.Serialization;

namespace Synaxis.InferenceGateway.Infrastructure.External.OpenAi.Dto
{
    public sealed class OpenAiModelDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("created")]
        public long? Created { get; set; }

        [JsonPropertyName("owned_by")]
        public string? OwnedBy { get; set; }
    }
}

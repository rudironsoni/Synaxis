using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Synaxis.WebApi.DTOs;

public class CompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";
    [JsonPropertyName("prompt")]
    public object? Prompt { get; set; } // string or array of strings
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
    [JsonPropertyName("user")]
    public string? User { get; set; }
    [JsonPropertyName("echo")]
    public bool Echo { get; set; }
    [JsonPropertyName("stop")]
    public object? Stop { get; set; } // string or array
    [JsonPropertyName("best_of")]
    public int? BestOf { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

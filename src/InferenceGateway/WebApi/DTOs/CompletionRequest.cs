using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Synaxis.InferenceGateway.WebApi.DTOs;

public class CompletionRequest
{
    [JsonPropertyName("model")]
    [Required(ErrorMessage = "Model is required")]
    public string Model { get; set; } = "";

    [JsonPropertyName("prompt")]
    [Required(ErrorMessage = "Prompt is required")]
    public object? Prompt { get; set; }

    [JsonPropertyName("max_tokens")]
    [Range(1, int.MaxValue, ErrorMessage = "MaxTokens must be a positive integer")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0 and 2")]
    public double? Temperature { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("user")]
    [StringLength(100, ErrorMessage = "User identifier must not exceed 100 characters")]
    public string? User { get; set; }

    [JsonPropertyName("echo")]
    public bool Echo { get; set; }

    [JsonPropertyName("stop")]
    public object? Stop { get; set; }

    [JsonPropertyName("best_of")]
    [Range(1, int.MaxValue, ErrorMessage = "BestOf must be a positive integer")]
    public int? BestOf { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

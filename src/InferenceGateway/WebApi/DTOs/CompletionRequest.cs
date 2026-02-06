// <copyright file="CompletionRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.DTOs
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request for completion endpoint.
    /// </summary>
    public class CompletionRequest
    {
    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    [JsonPropertyName("model")]
    [Required(ErrorMessage = "Model is required")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt for completion.
    /// </summary>
    [JsonPropertyName("prompt")]
    [Required(ErrorMessage = "Prompt is required")]
    public object? Prompt { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    [Range(1, int.MaxValue, ErrorMessage = "MaxTokens must be a positive integer")]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the sampling temperature.
    /// </summary>
    [JsonPropertyName("temperature")]
    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0 and 2")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to stream the response.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    [JsonPropertyName("user")]
    [StringLength(100, ErrorMessage = "User identifier must not exceed 100 characters")]
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to echo the prompt.
    /// </summary>
    [JsonPropertyName("echo")]
    public bool Echo { get; set; }

    /// <summary>
    /// Gets or sets the stop sequences.
    /// </summary>
    [JsonPropertyName("stop")]
    public object? Stop { get; set; }

    /// <summary>
    /// Gets or sets the number of completions to generate.
    /// </summary>
    [JsonPropertyName("best_of")]
    [Range(1, int.MaxValue, ErrorMessage = "BestOf must be a positive integer")]
    public int? BestOf { get; set; }

    /// <summary>
    /// Gets or sets additional extension data.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
    }

}
// <copyright file="ModelResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Api.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Response model for listing available models.
/// </summary>
public class ModelListResponse
{
    /// <summary>
    /// Gets or sets the object type.
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "list";

    /// <summary>
    /// Gets or sets the list of models.
    /// </summary>
    [JsonPropertyName("data")]
    public List<ModelInfo> Data { get; set; } = new();
}

/// <summary>
/// Represents information about a model.
/// </summary>
public class ModelInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the object type.
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "model";

    /// <summary>
    /// Gets or sets the Unix timestamp for when the model was created.
    /// </summary>
    [JsonPropertyName("created")]
    public long Created { get; set; }

    /// <summary>
    /// Gets or sets the organization that owns the model.
    /// </summary>
    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model capabilities.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public ModelCapabilities Capabilities { get; set; } = new();
}

/// <summary>
/// Represents model capabilities.
/// </summary>
public class ModelCapabilities
{
    /// <summary>
    /// Gets or sets a value indicating whether streaming is supported.
    /// </summary>
    [JsonPropertyName("streaming")]
    public bool Streaming { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether function calling is supported.
    /// </summary>
    [JsonPropertyName("function_calling")]
    public bool FunctionCalling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether vision is supported.
    /// </summary>
    [JsonPropertyName("vision")]
    public bool Vision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether JSON mode is supported.
    /// </summary>
    [JsonPropertyName("json_mode")]
    public bool JsonMode { get; set; }
}

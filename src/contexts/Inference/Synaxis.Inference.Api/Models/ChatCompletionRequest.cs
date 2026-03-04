// <copyright file="ChatCompletionRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Api.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Request model for chat completion endpoints.
/// </summary>
public class ChatCompletionRequest
{
    /// <summary>
    /// Gets or sets the model to use for completion.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of messages in the conversation.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to stream the response.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets the temperature for sampling.
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the top_p value for nucleus sampling.
    /// </summary>
    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }

    /// <summary>
    /// Gets or sets the list of stop sequences.
    /// </summary>
    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }

    /// <summary>
    /// Gets or sets the number of completions to generate.
    /// </summary>
    [JsonPropertyName("n")]
    public int? N { get; set; }

    /// <summary>
    /// Gets or sets the seed for deterministic sampling.
    /// </summary>
    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    /// <summary>
    /// Gets or sets the tools available to the model.
    /// </summary>
    [JsonPropertyName("tools")]
    public List<ToolDefinition>? Tools { get; set; }

    /// <summary>
    /// Gets or sets the tool choice.
    /// </summary>
    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }
}

/// <summary>
/// Represents a chat message.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Gets or sets the role of the message author.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the name of the author.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the tool calls.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Gets or sets the tool call ID.
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }
}

/// <summary>
/// Defines a tool available to the model.
/// </summary>
public class ToolDefinition
{
    /// <summary>
    /// Gets or sets the tool type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// Gets or sets the function definition.
    /// </summary>
    [JsonPropertyName("function")]
    public FunctionDefinition Function { get; set; } = new();
}

/// <summary>
/// Defines a function available to the model.
/// </summary>
public class FunctionDefinition
{
    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the function parameters.
    /// </summary>
    [JsonPropertyName("parameters")]
    public object? Parameters { get; set; }
}

/// <summary>
/// Represents a tool call.
/// </summary>
public class ToolCall
{
    /// <summary>
    /// Gets or sets the tool call ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tool type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// Gets or sets the function call.
    /// </summary>
    [JsonPropertyName("function")]
    public FunctionCall Function { get; set; } = new();
}

/// <summary>
/// Represents a function call.
/// </summary>
public class FunctionCall
{
    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function arguments as JSON.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

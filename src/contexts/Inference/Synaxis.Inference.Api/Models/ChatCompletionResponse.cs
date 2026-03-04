// <copyright file="ChatCompletionResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Api.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Response model for chat completion endpoints.
/// </summary>
public class ChatCompletionResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the completion.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the object type.
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion";

    /// <summary>
    /// Gets or sets the Unix timestamp for when the completion was created.
    /// </summary>
    [JsonPropertyName("created")]
    public long Created { get; set; }

    /// <summary>
    /// Gets or sets the model used for the completion.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of choices.
    /// </summary>
    [JsonPropertyName("choices")]
    public List<ChatCompletionChoice> Choices { get; set; } = new();

    /// <summary>
    /// Gets or sets the usage information.
    /// </summary>
    [JsonPropertyName("usage")]
    public ChatCompletionUsage? Usage { get; set; }
}

/// <summary>
/// Represents a choice in a chat completion response.
/// </summary>
public class ChatCompletionChoice
{
    /// <summary>
    /// Gets or sets the index of the choice.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    [JsonPropertyName("message")]
    public ChatCompletionMessage Message { get; set; } = new();

    /// <summary>
    /// Gets or sets the finish reason.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    /// <summary>
    /// Gets or sets the logprobs.
    /// </summary>
    [JsonPropertyName("logprobs")]
    public object? Logprobs { get; set; }
}

/// <summary>
/// Represents a message in a chat completion response.
/// </summary>
public class ChatCompletionMessage
{
    /// <summary>
    /// Gets or sets the role of the message author.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the tool calls.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Gets or sets the refusal.
    /// </summary>
    [JsonPropertyName("refusal")]
    public string? Refusal { get; set; }
}

/// <summary>
/// Represents usage information in a chat completion response.
/// </summary>
public class ChatCompletionUsage
{
    /// <summary>
    /// Gets or sets the number of prompt tokens.
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of completion tokens.
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens.
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// Represents a streaming chunk in a chat completion response.
/// </summary>
public class ChatCompletionChunk
{
    /// <summary>
    /// Gets or sets the unique identifier for the completion.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the object type.
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion.chunk";

    /// <summary>
    /// Gets or sets the Unix timestamp for when the completion was created.
    /// </summary>
    [JsonPropertyName("created")]
    public long Created { get; set; }

    /// <summary>
    /// Gets or sets the model used for the completion.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of choices.
    /// </summary>
    [JsonPropertyName("choices")]
    public List<ChatCompletionChunkChoice> Choices { get; set; } = new();
}

/// <summary>
/// Represents a choice in a streaming chat completion chunk.
/// </summary>
public class ChatCompletionChunkChoice
{
    /// <summary>
    /// Gets or sets the index of the choice.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the delta.
    /// </summary>
    [JsonPropertyName("delta")]
    public ChatCompletionChunkDelta Delta { get; set; } = new();

    /// <summary>
    /// Gets or sets the finish reason.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// Represents a delta in a streaming chat completion chunk.
/// </summary>
public class ChatCompletionChunkDelta
{
    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the tool calls.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
}

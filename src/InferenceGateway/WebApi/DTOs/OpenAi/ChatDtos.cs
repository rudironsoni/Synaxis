// <copyright file="ChatDtos.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.DTOs.OpenAi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request for chat completion.
    /// </summary>
    public class ChatCompletionRequest
    {
        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of messages.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<ChatCompletionMessageDto> Messages { get; set; } = new ();

        /// <summary>
        /// Gets or sets a value indicating whether to stream the response.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Gets or sets the sampling temperature.
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of tokens.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets the nucleus sampling parameter.
        /// </summary>
        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }

        /// <summary>
        /// Gets or sets the list of tools.
        /// </summary>
        [JsonPropertyName("tools")]
        public List<object>? Tools { get; set; }
    }

    /// <summary>
    /// Chat completion message DTO.
    /// </summary>
    public class ChatCompletionMessageDto
    {
        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        [JsonPropertyName("content")]
        public object? Content { get; set; } // string or list

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the tool calls.
        /// </summary>
        [JsonPropertyName("tool_calls")]
        public List<object>? ToolCalls { get; set; }
    }

    /// <summary>
    /// Chat completion response.
    /// </summary>
    public class ChatCompletionResponse
    {
        /// <summary>
        /// Gets or sets the response ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion";

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; set; } = new ();

        /// <summary>
        /// Gets or sets the usage statistics.
        /// </summary>
        [JsonPropertyName("usage")]
        public ChatCompletionUsage? Usage { get; set; }
    }

    /// <summary>
    /// Chat completion choice.
    /// </summary>
    public class ChatCompletionChoice
    {
        /// <summary>
        /// Gets or sets the choice index.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [JsonPropertyName("message")]
        public ChatCompletionMessageDto Message { get; set; } = new ();

        /// <summary>
        /// Gets or sets the finish reason.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// Chat completion usage statistics.
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
    /// Chat completion chunk for streaming.
    /// </summary>
    public class ChatCompletionChunk
    {
        /// <summary>
        /// Gets or sets the chunk ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion.chunk";

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<ChatCompletionChunkChoice> Choices { get; set; } = new ();
    }

    /// <summary>
    /// Chat completion chunk choice.
    /// </summary>
    public class ChatCompletionChunkChoice
    {
        /// <summary>
        /// Gets or sets the choice index.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the delta.
        /// </summary>
        [JsonPropertyName("delta")]
        public ChatCompletionChunkDelta Delta { get; set; } = new ();

        /// <summary>
        /// Gets or sets the finish reason.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// Chat completion chunk delta.
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
        public List<object>? ToolCalls { get; set; }
    }
}

// <copyright file="JsonSerializationBenchmarks.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Benchmarks;

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Synaxis.InferenceGateway.Application.Configuration;

/// <summary>
/// Benchmarks for JSON serialization performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class JsonSerializationBenchmarks
{
    private JsonSerializerOptions _options = null!;
    private string _smallRequestJson = null!;
    private string _mediumRequestJson = null!;
    private string _largeRequestJson = null!;
    private string _smallResponseJson = null!;
    private string _mediumResponseJson = null!;
    private string _largeResponseJson = null!;
    private ChatCompletionRequest _smallRequest = null!;
    private ChatCompletionRequest _mediumRequest = null!;
    private ChatCompletionRequest _largeRequest = null!;
    private ChatCompletionResponse _smallResponse = null!;
    private ChatCompletionResponse _mediumResponse = null!;
    private ChatCompletionResponse _largeResponse = null!;

    /// <summary>
    /// Sets up the benchmark data.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        this._options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        this._smallRequest = this.CreateChatCompletionRequest(1);
        this._mediumRequest = this.CreateChatCompletionRequest(10);
        this._largeRequest = this.CreateChatCompletionRequest(100);

        this._smallResponse = this.CreateChatCompletionResponse(1);
        this._mediumResponse = this.CreateChatCompletionResponse(10);
        this._largeResponse = this.CreateChatCompletionResponse(100);

        this._smallRequestJson = JsonSerializer.Serialize(this._smallRequest, this._options);
        this._mediumRequestJson = JsonSerializer.Serialize(this._mediumRequest, this._options);
        this._largeRequestJson = JsonSerializer.Serialize(this._largeRequest, this._options);

        this._smallResponseJson = JsonSerializer.Serialize(this._smallResponse, this._options);
        this._mediumResponseJson = JsonSerializer.Serialize(this._mediumResponse, this._options);
        this._largeResponseJson = JsonSerializer.Serialize(this._largeResponse, this._options);
    }

    /// <summary>
    /// Benchmarks serialization of a small request.
    /// </summary>
    /// <returns>The serialized JSON string.</returns>
    [Benchmark]
    public string Serialize_SmallRequest()
    {
        return JsonSerializer.Serialize(this._smallRequest, this._options);
    }

    /// <summary>
    /// Benchmarks serialization of a medium request.
    /// </summary>
    /// <returns>The serialized JSON string.</returns>
    [Benchmark]
    public string Serialize_MediumRequest()
    {
        return JsonSerializer.Serialize(this._mediumRequest, this._options);
    }

    /// <summary>
    /// Benchmarks serialization of a large request.
    /// </summary>
    /// <returns>The serialized JSON string.</returns>
    [Benchmark]
    public string Serialize_LargeRequest()
    {
        return JsonSerializer.Serialize(this._largeRequest, this._options);
    }

    /// <summary>
    /// Benchmarks serialization of a small response.
    /// </summary>
    /// <returns>The serialized JSON string.</returns>
    [Benchmark]
    public string Serialize_SmallResponse()
    {
        return JsonSerializer.Serialize(this._smallResponse, this._options);
    }

    /// <summary>
    /// Benchmarks serialization of a medium response.
    /// </summary>
    /// <returns>The serialized JSON string.</returns>
    [Benchmark]
    public string Serialize_MediumResponse()
    {
        return JsonSerializer.Serialize(this._mediumResponse, this._options);
    }

    /// <summary>
    /// Benchmarks serialization of a large response.
    /// </summary>
    /// <returns>The serialized JSON string.</returns>
    [Benchmark]
    public string Serialize_LargeResponse()
    {
        return JsonSerializer.Serialize(this._largeResponse, this._options);
    }

    /// <summary>
    /// Benchmarks deserialization of a small request.
    /// </summary>
    /// <returns>The deserialized request.</returns>
    [Benchmark]
    public ChatCompletionRequest Deserialize_SmallRequest()
    {
        return JsonSerializer.Deserialize<ChatCompletionRequest>(this._smallRequestJson, this._options)!;
    }

    /// <summary>
    /// Benchmarks deserialization of a medium request.
    /// </summary>
    /// <returns>The deserialized request.</returns>
    [Benchmark]
    public ChatCompletionRequest Deserialize_MediumRequest()
    {
        return JsonSerializer.Deserialize<ChatCompletionRequest>(this._mediumRequestJson, this._options)!;
    }

    /// <summary>
    /// Benchmarks deserialization of a large request.
    /// </summary>
    /// <returns>The deserialized request.</returns>
    [Benchmark]
    public ChatCompletionRequest Deserialize_LargeRequest()
    {
        return JsonSerializer.Deserialize<ChatCompletionRequest>(this._largeRequestJson, this._options)!;
    }

    /// <summary>
    /// Benchmarks deserialization of a small response.
    /// </summary>
    /// <returns>The deserialized response.</returns>
    [Benchmark]
    public ChatCompletionResponse Deserialize_SmallResponse()
    {
        return JsonSerializer.Deserialize<ChatCompletionResponse>(this._smallResponseJson, this._options)!;
    }

    /// <summary>
    /// Benchmarks deserialization of a medium response.
    /// </summary>
    /// <returns>The deserialized response.</returns>
    [Benchmark]
    public ChatCompletionResponse Deserialize_MediumResponse()
    {
        return JsonSerializer.Deserialize<ChatCompletionResponse>(this._mediumResponseJson, this._options)!;
    }

    /// <summary>
    /// Benchmarks deserialization of a large response.
    /// </summary>
    /// <returns>The deserialized response.</returns>
    [Benchmark]
    public ChatCompletionResponse Deserialize_LargeResponse()
    {
        return JsonSerializer.Deserialize<ChatCompletionResponse>(this._largeResponseJson, this._options)!;
    }

    /// <summary>
    /// Benchmarks serialization and deserialization of a small request.
    /// </summary>
    /// <returns>The deserialized request.</returns>
    [Benchmark]
    public ChatCompletionRequest SerializeDeserialize_SmallRequest()
    {
        var json = JsonSerializer.Serialize(this._smallRequest, this._options);
        return JsonSerializer.Deserialize<ChatCompletionRequest>(json, this._options)!;
    }

    /// <summary>
    /// Benchmarks serialization and deserialization of a medium request.
    /// </summary>
    /// <returns>The deserialized request.</returns>
    [Benchmark]
    public ChatCompletionRequest SerializeDeserialize_MediumRequest()
    {
        var json = JsonSerializer.Serialize(this._mediumRequest, this._options);
        return JsonSerializer.Deserialize<ChatCompletionRequest>(json, this._options)!;
    }

    /// <summary>
    /// Benchmarks serialization and deserialization of a large request.
    /// </summary>
    /// <returns>The deserialized request.</returns>
    [Benchmark]
    public ChatCompletionRequest SerializeDeserialize_LargeRequest()
    {
        var json = JsonSerializer.Serialize(this._largeRequest, this._options);
        return JsonSerializer.Deserialize<ChatCompletionRequest>(json, this._options)!;
    }

    private ChatCompletionRequest CreateChatCompletionRequest(int messageCount)
    {
        var messages = new List<ChatMessage>();
        for (int i = 0; i < messageCount; i++)
        {
            messages.Add(new ChatMessage
            {
                Role = i % 2 == 0 ? "user" : "assistant",
                Content = $"This is message number {i + 1} with some content to simulate a realistic chat conversation.",
            });
        }

        return new ChatCompletionRequest
        {
            Model = "gpt-4",
            Messages = messages,
            Temperature = 0.7,
            MaxTokens = 1000,
            Stream = false,
        };
    }

    private ChatCompletionResponse CreateChatCompletionResponse(int messageCount)
    {
        var messages = new List<ChatMessage>();
        for (int i = 0; i < messageCount; i++)
        {
            messages.Add(new ChatMessage
            {
                Role = i % 2 == 0 ? "user" : "assistant",
                Content = $"This is response message number {i + 1} with some content to simulate a realistic chat response.",
            });
        }

        return new ChatCompletionResponse
        {
            Id = "chatcmpl-" + Guid.NewGuid().ToString(),
            Object = "chat.completion",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = "gpt-4",
            Choices = new List<Choice>
            {
                new Choice
                {
                    Index = 0,
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = "This is the final assistant response with detailed content.",
                    },
                    FinishReason = "stop",
                },
            },
            Usage = new Usage
            {
                PromptTokens = 100 * messageCount,
                CompletionTokens = 50 * messageCount,
                TotalTokens = 150 * messageCount,
            },
        };
    }

    /// <summary>
    /// Represents a chat completion request.
    /// </summary>
    public class ChatCompletionRequest
    {
        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of chat messages.
        /// </summary>
        public IReadOnlyList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// Gets or sets the temperature for sampling.
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of tokens.
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to stream the response.
        /// </summary>
        public bool? Stream { get; set; }
    }

    /// <summary>
    /// Represents a chat completion response.
    /// </summary>
    public class ChatCompletionResponse
    {
        /// <summary>
        /// Gets or sets the response identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the response choices.
        /// </summary>
        public IReadOnlyList<Choice> Choices { get; set; } = new List<Choice>();

        /// <summary>
        /// Gets or sets the token usage.
        /// </summary>
        public Usage? Usage { get; set; }
    }

    /// <summary>
    /// Represents a chat message.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Gets or sets the message role.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a completion choice.
    /// </summary>
    public class Choice
    {
        /// <summary>
        /// Gets or sets the choice index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public ChatMessage? Message { get; set; }

        /// <summary>
        /// Gets or sets the finish reason.
        /// </summary>
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// Represents token usage.
    /// </summary>
    public class Usage
    {
        /// <summary>
        /// Gets or sets the prompt tokens.
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// Gets or sets the completion tokens.
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Gets or sets the total tokens.
        /// </summary>
        public int TotalTokens { get; set; }
    }
}

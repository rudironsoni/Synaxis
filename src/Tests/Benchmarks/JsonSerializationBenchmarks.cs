// <copyright file="JsonSerializationBenchmarks.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Benchmarks;

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Synaxis.InferenceGateway.Application.Configuration;

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

    [Benchmark]
    public string Serialize_SmallRequest()
    {
        return JsonSerializer.Serialize(this._smallRequest, this._options);
    }

    [Benchmark]
    public string Serialize_MediumRequest()
    {
        return JsonSerializer.Serialize(this._mediumRequest, this._options);
    }

    [Benchmark]
    public string Serialize_LargeRequest()
    {
        return JsonSerializer.Serialize(this._largeRequest, this._options);
    }

    [Benchmark]
    public string Serialize_SmallResponse()
    {
        return JsonSerializer.Serialize(this._smallResponse, this._options);
    }

    [Benchmark]
    public string Serialize_MediumResponse()
    {
        return JsonSerializer.Serialize(this._mediumResponse, this._options);
    }

    [Benchmark]
    public string Serialize_LargeResponse()
    {
        return JsonSerializer.Serialize(this._largeResponse, this._options);
    }

    [Benchmark]
    public ChatCompletionRequest Deserialize_SmallRequest()
    {
        return JsonSerializer.Deserialize<ChatCompletionRequest>(this._smallRequestJson, this._options) !;
    }

    [Benchmark]
    public ChatCompletionRequest Deserialize_MediumRequest()
    {
        return JsonSerializer.Deserialize<ChatCompletionRequest>(this._mediumRequestJson, this._options) !;
    }

    [Benchmark]
    public ChatCompletionRequest Deserialize_LargeRequest()
    {
        return JsonSerializer.Deserialize<ChatCompletionRequest>(this._largeRequestJson, this._options) !;
    }

    [Benchmark]
    public ChatCompletionResponse Deserialize_SmallResponse()
    {
        return JsonSerializer.Deserialize<ChatCompletionResponse>(this._smallResponseJson, this._options) !;
    }

    [Benchmark]
    public ChatCompletionResponse Deserialize_MediumResponse()
    {
        return JsonSerializer.Deserialize<ChatCompletionResponse>(this._mediumResponseJson, this._options) !;
    }

    [Benchmark]
    public ChatCompletionResponse Deserialize_LargeResponse()
    {
        return JsonSerializer.Deserialize<ChatCompletionResponse>(this._largeResponseJson, this._options) !;
    }

    [Benchmark]
    public ChatCompletionRequest SerializeDeserialize_SmallRequest()
    {
        var json = JsonSerializer.Serialize(this._smallRequest, this._options);
        return JsonSerializer.Deserialize<ChatCompletionRequest>(json, this._options) !;
    }

    [Benchmark]
    public ChatCompletionRequest SerializeDeserialize_MediumRequest()
    {
        var json = JsonSerializer.Serialize(this._mediumRequest, this._options);
        return JsonSerializer.Deserialize<ChatCompletionRequest>(json, this._options) !;
    }

    [Benchmark]
    public ChatCompletionRequest SerializeDeserialize_LargeRequest()
    {
        var json = JsonSerializer.Serialize(this._largeRequest, this._options);
        return JsonSerializer.Deserialize<ChatCompletionRequest>(json, this._options) !;
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

    public class ChatCompletionRequest
    {
        public string Model { get; set; } = string.Empty;

        public IReadOnlyList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public double? Temperature { get; set; }

        public int? MaxTokens { get; set; }

        public bool? Stream { get; set; }
    }

    public class ChatCompletionResponse
    {
        public string Id { get; set; } = string.Empty;

        public string Object { get; set; } = string.Empty;

        public long Created { get; set; }

        public string Model { get; set; } = string.Empty;

        public IReadOnlyList<Choice> Choices { get; set; } = new List<Choice>();

        public Usage? Usage { get; set; }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }

    public class Choice
    {
        public int Index { get; set; }

        public ChatMessage? Message { get; set; }

        public string? FinishReason { get; set; }
    }

    public class Usage
    {
        public int PromptTokens { get; set; }

        public int CompletionTokens { get; set; }

        public int TotalTokens { get; set; }
    }
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using System.Text.Json;
using Synaxis.InferenceGateway.Application.Configuration;

namespace Synaxis.Benchmarks;

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
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        _smallRequest = CreateChatCompletionRequest(1);
        _mediumRequest = CreateChatCompletionRequest(10);
        _largeRequest = CreateChatCompletionRequest(100);

        _smallResponse = CreateChatCompletionResponse(1);
        _mediumResponse = CreateChatCompletionResponse(10);
        _largeResponse = CreateChatCompletionResponse(100);

        _smallRequestJson = JsonSerializer.Serialize(_smallRequest, _options);
        _mediumRequestJson = JsonSerializer.Serialize(_mediumRequest, _options);
        _largeRequestJson = JsonSerializer.Serialize(_largeRequest, _options);

        _smallResponseJson = JsonSerializer.Serialize(_smallResponse, _options);
        _mediumResponseJson = JsonSerializer.Serialize(_mediumResponse, _options);
        _largeResponseJson = JsonSerializer.Serialize(_largeResponse, _options);
    }

    [Benchmark]
    public string Serialize_SmallRequest()
    {
        return JsonSerializer.Serialize(_smallRequest, _options);
    }

    [Benchmark]
    public string Serialize_MediumRequest()
    {
        return JsonSerializer.Serialize(_mediumRequest, _options);
    }

    [Benchmark]
    public string Serialize_LargeRequest()
    {
        return JsonSerializer.Serialize(_largeRequest, _options);
    }

    [Benchmark]
    public string Serialize_SmallResponse()
    {
        return JsonSerializer.Serialize(_smallResponse, _options);
    }

    [Benchmark]
    public string Serialize_MediumResponse()
    {
        return JsonSerializer.Serialize(_mediumResponse, _options);
    }

    [Benchmark]
    public string Serialize_LargeResponse()
    {
        return JsonSerializer.Serialize(_largeResponse, _options);
    }

    [Benchmark]
    public ChatCompletionRequest Deserialize_SmallRequest()
    {
        return JsonSerializer.Deserialize<ChatCompletionRequest>(_smallRequestJson, _options)!;
    }

    [Benchmark]
    public ChatCompletionRequest Deserialize_MediumRequest()
    {
        return JsonSerializer.Deserialize<ChatCompletionRequest>(_mediumRequestJson, _options)!;
    }

    [Benchmark]
    public ChatCompletionRequest Deserialize_LargeRequest()
    {
        return JsonSerializer.Deserialize<ChatCompletionRequest>(_largeRequestJson, _options)!;
    }

    [Benchmark]
    public ChatCompletionResponse Deserialize_SmallResponse()
    {
        return JsonSerializer.Deserialize<ChatCompletionResponse>(_smallResponseJson, _options)!;
    }

    [Benchmark]
    public ChatCompletionResponse Deserialize_MediumResponse()
    {
        return JsonSerializer.Deserialize<ChatCompletionResponse>(_mediumResponseJson, _options)!;
    }

    [Benchmark]
    public ChatCompletionResponse Deserialize_LargeResponse()
    {
        return JsonSerializer.Deserialize<ChatCompletionResponse>(_largeResponseJson, _options)!;
    }

    [Benchmark]
    public ChatCompletionRequest SerializeDeserialize_SmallRequest()
    {
        var json = JsonSerializer.Serialize(_smallRequest, _options);
        return JsonSerializer.Deserialize<ChatCompletionRequest>(json, _options)!;
    }

    [Benchmark]
    public ChatCompletionRequest SerializeDeserialize_MediumRequest()
    {
        var json = JsonSerializer.Serialize(_mediumRequest, _options);
        return JsonSerializer.Deserialize<ChatCompletionRequest>(json, _options)!;
    }

    [Benchmark]
    public ChatCompletionRequest SerializeDeserialize_LargeRequest()
    {
        var json = JsonSerializer.Serialize(_largeRequest, _options);
        return JsonSerializer.Deserialize<ChatCompletionRequest>(json, _options)!;
    }

    private ChatCompletionRequest CreateChatCompletionRequest(int messageCount)
    {
        var messages = new List<ChatMessage>();
        for (int i = 0; i < messageCount; i++)
        {
            messages.Add(new ChatMessage
            {
                Role = i % 2 == 0 ? "user" : "assistant",
                Content = $"This is message number {i + 1} with some content to simulate a realistic chat conversation."
            });
        }

        return new ChatCompletionRequest
        {
            Model = "gpt-4",
            Messages = messages,
            Temperature = 0.7,
            MaxTokens = 1000,
            Stream = false
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
                Content = $"This is response message number {i + 1} with some content to simulate a realistic chat response."
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
                        Content = "This is the final assistant response with detailed content."
                    },
                    FinishReason = "stop"
                }
            },
            Usage = new Usage
            {
                PromptTokens = 100 * messageCount,
                CompletionTokens = 50 * messageCount,
                TotalTokens = 150 * messageCount
            }
        };
    }

    public class ChatCompletionRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<ChatMessage> Messages { get; set; } = new();
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
        public List<Choice> Choices { get; set; } = new();
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

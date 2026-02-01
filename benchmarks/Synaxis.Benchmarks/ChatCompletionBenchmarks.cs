using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[Config(typeof(BenchmarkConfig))]
public class ChatCompletionBenchmarks
{
    private List<ChatMessage> _singleMessage = null!;
    private List<ChatMessage> _multipleMessages = null!;
    private List<ChatMessage> _longMessage = null!;
    private ChatOptions _options = null!;
    private CancellationToken _cancellationToken;

    [GlobalSetup]
    public void Setup()
    {
        _cancellationToken = CancellationToken.None;
        _options = new ChatOptions { ModelId = "test-model" };

        _singleMessage = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello, how are you today?")
        };

        _multipleMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "What is the capital of France?"),
            new ChatMessage(ChatRole.Assistant, "The capital of France is Paris."),
            new ChatMessage(ChatRole.User, "And what about Germany?")
        };

        var longContent = string.Join(" ", Enumerable.Repeat("This is a test message. ", 100));
        _longMessage = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, longContent)
        };
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public List<ChatMessage> CreateSingleMessage()
    {
        return new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello, how are you today?")
        };
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public List<ChatMessage> CreateMultipleMessages()
    {
        return new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "What is the capital of France?"),
            new ChatMessage(ChatRole.Assistant, "The capital of France is Paris."),
            new ChatMessage(ChatRole.User, "And what about Germany?")
        };
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public List<ChatMessage> CreateLongMessage()
    {
        var longContent = string.Join(" ", Enumerable.Repeat("This is a test message. ", 100));
        return new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, longContent)
        };
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public ChatOptions CreateChatOptions()
    {
        return new ChatOptions
        {
            ModelId = "test-model",
            Temperature = 0.7f,
            MaxOutputTokens = 1000,
            TopP = 0.9f
        };
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public List<string> CreateStreamingChunks()
    {
        var chunks = new[] { "Hello", " there", "!", " How", " can", " I", " help", " you", "?" };
        return chunks.ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public async Task<List<string>> SimulateStreamingResponse()
    {
        var chunks = new List<string>();
        var chunkTexts = new[] { "Hello", " there", "!", " How", " can", " I", " help", " you", "?" };

        foreach (var chunk in chunkTexts)
        {
            await Task.Delay(1, _cancellationToken);
            chunks.Add(chunk);
        }

        return chunks;
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public List<ChatMessage> FilterMessagesByRole()
    {
        return _multipleMessages
            .Where(m => m.Role == ChatRole.User)
            .ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public int CountTokens_Simple()
    {
        var message = "Hello, how are you today?";
        return message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public int CountTokens_Long()
    {
        var longContent = string.Join(" ", Enumerable.Repeat("This is a test message. ", 100));
        return longContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public Dictionary<string, object> CreateResponseMetadata()
    {
        return new Dictionary<string, object>
        {
            ["provider_name"] = "TestProvider",
            ["model_id"] = "test-model",
            ["request_id"] = Guid.NewGuid().ToString(),
            ["latency_ms"] = 150
        };
    }

    [Benchmark]
    [BenchmarkCategory("Chat")]
    public UsageDetails CreateUsageDetails()
    {
        return new UsageDetails
        {
            InputTokenCount = 10,
            OutputTokenCount = 20,
            TotalTokenCount = 30
        };
    }
}

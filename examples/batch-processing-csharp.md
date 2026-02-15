# Batch Processing Example (C#)

This example demonstrates how to use Synaxis for batch processing multiple AI requests efficiently using C#.

## Prerequisites

- **.NET 8 SDK** or later
- **Synaxis SDK** packages
- **API Key** for at least one AI provider

## Setup

### Create Project

```bash
dotnet new console -n SynaxisBatchProcessing
cd SynaxisBatchProcessing
```

### Install Packages

```bash
dotnet add package Synaxis
dotnet add package Synaxis.Providers.OpenAI
```

### Program.cs Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Synaxis;
using Synaxis.Providers.OpenAI;

var host = Host.CreateApplicationBuilder(args);

// Configure Synaxis
host.Services.AddSynaxis(options =>
{
    options.AddOpenAIProvider(apiKey: "sk-your-openai-key");
});

var app = host.Build();
```

## Basic Batch Processing

### Process Multiple Requests Sequentially

```csharp
using Synaxis.Contracts;

public class BatchProcessor
{
    private readonly IChatService _chatService;

    public BatchProcessor(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<List<string>> ProcessSequentialAsync(List<string> prompts)
    {
        var results = new List<string>();

        foreach (var prompt in prompts)
        {
            var request = new ChatCompletionRequest
            {
                Model = "gpt-4",
                Messages = new[]
                {
                    new ChatMessage { Role = "user", Content = prompt }
                }
            };

            var response = await _chatService.CompleteAsync(request);
            results.Add(response.Choices[0].Message.Content);
        }

        return results;
    }
}

// Usage
var processor = app.Services.GetRequiredService<BatchProcessor>();
var prompts = new List<string>
{
    "What is the capital of France?",
    "Explain quantum computing.",
    "Write a haiku about programming."
};

var results = await processor.ProcessSequentialAsync(prompts);

for (int i = 0; i < prompts.Count; i++)
{
    Console.WriteLine($"Prompt: {prompts[i]}");
    Console.WriteLine($"Response: {results[i]}\n");
}
```

### Process Multiple Requests Concurrently

```csharp
public class ConcurrentBatchProcessor
{
    private readonly IChatService _chatService;

    public ConcurrentBatchProcessor(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<List<string>> ProcessConcurrentAsync(
        List<string> prompts,
        int maxConcurrency = 5)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = prompts.Select(async prompt =>
        {
            await semaphore.WaitAsync();
            try
            {
                var request = new ChatCompletionRequest
                {
                    Model = "gpt-4",
                    Messages = new[]
                    {
                        new ChatMessage { Role = "user", Content = prompt }
                    }
                };

                var response = await _chatService.CompleteAsync(request);
                return response.Choices[0].Message.Content;
            }
            finally
            {
                semaphore.Release();
            }
        });

        return (await Task.WhenAll(tasks)).ToList();
    }
}

// Usage
var concurrentProcessor = app.Services.GetRequiredService<ConcurrentBatchProcessor>();
var results = await concurrentProcessor.ProcessConcurrentAsync(prompts, maxConcurrency: 3);
```

## Batch API

### Using the Batch Endpoint

```csharp
using System.Text.Json;

public class BatchApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public BatchApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"];
        _httpClient.BaseAddress = new Uri("http://localhost:8080");
    }

    public async Task<string> CreateBatchAsync(List<BatchRequestItem> requests)
    {
        var batchRequest = new
        {
            requests = requests
        };

        var content = new StringContent(
            JsonSerializer.Serialize(batchRequest),
            Encoding.UTF8,
            "application/json"
        );

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsync("/v1/batch", content);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(data);
        return result.GetProperty("id").GetString();
    }

    public async Task<BatchStatus> GetBatchStatusAsync(string batchId)
    {
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.GetAsync($"/v1/batch/{batchId}");
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BatchStatus>(data);
    }

    public async Task<List<BatchResult>> GetBatchResultsAsync(string batchId)
    {
        // Wait for batch to complete
        BatchStatus status;
        do
        {
            status = await GetBatchStatusAsync(batchId);
            if (status.Status != "completed")
            {
                await Task.Delay(1000);
            }
        } while (status.Status != "completed");

        // Retrieve results
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.GetAsync($"/v1/batch/{batchId}/results");
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<BatchResult>>(data);
    }
}

// Data models
public record BatchRequestItem(
    string CustomId,
    string Method,
    string Url,
    ChatCompletionRequest Body
);

public record BatchStatus(
    string Id,
    string Status,
    int Completed,
    int Failed,
    int Total
);

public record BatchResult(
    string CustomId,
    ChatCompletionResponse Response,
    Error Error
);

// Usage
var batchClient = app.Services.GetRequiredService<BatchApiClient>();

var batchRequests = prompts.Select((prompt, index) => new BatchRequestItem(
    CustomId: $"req-{index}",
    Method: "POST",
    Url: "/v1/chat/completions",
    Body: new ChatCompletionRequest
    {
        Model = "gpt-4",
        Messages = new[]
        {
            new ChatMessage { Role = "user", Content = prompt }
        }
    }
)).ToList();

var batchId = await batchClient.CreateBatchAsync(batchRequests);
Console.WriteLine($"Batch created: {batchId}");

var results = await batchClient.GetBatchResultsAsync(batchId);

foreach (var result in results)
{
    Console.WriteLine($"Request: {result.CustomId}");
    if (result.Response != null)
    {
        Console.WriteLine($"Response: {result.Response.Choices[0].Message.Content}\n");
    }
    else if (result.Error != null)
    {
        Console.WriteLine($"Error: {result.Error.Message}\n");
    }
}
```

## Advanced Batch Processing

### Batch with Progress Tracking

```csharp
public class BatchProcessorWithProgress
{
    private readonly IChatService _chatService;
    private readonly ILogger<BatchProcessorWithProgress> _logger;

    public BatchProcessorWithProgress(
        IChatService chatService,
        ILogger<BatchProcessorWithProgress> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task<BatchResult> ProcessWithProgressAsync(
        List<string> prompts,
        IProgress<BatchProgress> progress,
        CancellationToken cancellationToken = default)
    {
        var total = prompts.Count;
        var completed = 0;
        var results = new List<string>();
        var errors = new List<(string Prompt, string Error)>();

        foreach (var prompt in prompts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var request = new ChatCompletionRequest
                {
                    Model = "gpt-4",
                    Messages = new[]
                    {
                        new ChatMessage { Role = "user", Content = prompt }
                    }
                };

                var response = await _chatService.CompleteAsync(request, cancellationToken);
                results.Add(response.Choices[0].Message.Content);

                completed++;
                progress?.Report(new BatchProgress(
                    Total: total,
                    Completed: completed,
                    Failed: errors.Count,
                    CurrentPrompt: prompt
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing prompt: {Prompt}", prompt);
                errors.Add((prompt, ex.Message));

                progress?.Report(new BatchProgress(
                    Total: total,
                    Completed: completed,
                    Failed: errors.Count,
                    CurrentPrompt: prompt,
                    Error: ex.Message
                ));
            }
        }

        return new BatchResult(
            Total: total,
            Completed: completed,
            Failed: errors.Count,
            Results: results,
            Errors: errors
        );
    }
}

// Data models
public record BatchProgress(
    int Total,
    int Completed,
    int Failed,
    string CurrentPrompt,
    string Error = null
);

public record BatchResult(
    int Total,
    int Completed,
    int Failed,
    List<string> Results,
    List<(string Prompt, string Error)> Errors
);

// Usage
var processor = app.Services.GetRequiredService<BatchProcessorWithProgress>();

var progress = new Progress<BatchProgress>(p =>
{
    Console.WriteLine($"Progress: {p.Completed}/{p.Total} completed, {p.Failed} failed");
    if (p.Error != null)
    {
        Console.WriteLine($"  Error: {p.Error}");
    }
});

var result = await processor.ProcessWithProgressAsync(prompts, progress);

Console.WriteLine($"\nBatch completed: {result.Completed} succeeded, {result.Failed} failed");
```

### Batch with Retry Logic

```csharp
public class BatchProcessorWithRetry
{
    private readonly IChatService _chatService;
    private readonly ILogger<BatchProcessorWithRetry> _logger;

    public BatchProcessorWithRetry(
        IChatService chatService,
        ILogger<BatchProcessorWithRetry> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task<List<string>> ProcessWithRetryAsync(
        List<string> prompts,
        int maxRetries = 3,
        TimeSpan? retryDelay = null)
    {
        var results = new List<string>();
        var retryDelay = retryDelay ?? TimeSpan.FromSeconds(2);

        foreach (var prompt in prompts)
        {
            var attempt = 0;
            var success = false;

            while (attempt < maxRetries && !success)
            {
                try
                {
                    var request = new ChatCompletionRequest
                    {
                        Model = "gpt-4",
                        Messages = new[]
                        {
                            new ChatMessage { Role = "user", Content = prompt }
                        }
                    };

                    var response = await _chatService.CompleteAsync(request);
                    results.Add(response.Choices[0].Message.Content);
                    success = true;
                }
                catch (Exception ex) when (attempt < maxRetries - 1)
                {
                    attempt++;
                    _logger.LogWarning(
                        ex,
                        "Attempt {Attempt}/{MaxRetries} failed for prompt. Retrying...",
                        attempt,
                        maxRetries
                    );

                    // Exponential backoff
                    var delay = retryDelay * Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "All retries failed for prompt");
                    results.Add($"Error: {ex.Message}");
                    success = true; // Move to next prompt
                }
            }
        }

        return results;
    }
}

// Usage
var retryProcessor = app.Services.GetRequiredService<BatchProcessorWithRetry>();
var results = await retryProcessor.ProcessWithRetryAsync(prompts, maxRetries: 3);
```

### Batch with Rate Limiting

```csharp
public class RateLimitedBatchProcessor
{
    private readonly IChatService _chatService;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly ILogger<RateLimitedBatchProcessor> _logger;

    public RateLimitedBatchProcessor(
        IChatService chatService,
        ILogger<RateLimitedBatchProcessor> logger)
    {
        _chatService = chatService;
        _logger = logger;
        // Allow 10 requests per second
        _rateLimiter = new SemaphoreSlim(10, 10);
    }

    public async Task<List<string>> ProcessRateLimitedAsync(
        List<string> prompts,
        int requestsPerSecond = 10)
    {
        var results = new List<string>();
        var tasks = prompts.Select(async (prompt, index) =>
        {
            await _rateLimiter.WaitAsync();
            try
            {
                var request = new ChatCompletionRequest
                {
                    Model = "gpt-4",
                    Messages = new[]
                    {
                        new ChatMessage { Role = "user", Content = prompt }
                    }
                };

                var response = await _chatService.CompleteAsync(request);
                return response.Choices[0].Message.Content;
            }
            finally
            {
                // Release after 1 second to maintain rate limit
                _ = Task.Delay(1000).ContinueWith(_ => _rateLimiter.Release());
            }
        });

        return (await Task.WhenAll(tasks)).ToList();
    }
}

// Usage
var rateLimitedProcessor = app.Services.GetRequiredService<RateLimitedBatchProcessor>();
var results = await rateLimitedProcessor.ProcessRateLimitedAsync(prompts, requestsPerSecond: 10);
```

## Batch Processing with File I/O

### Process Prompts from File

```csharp
public class FileBatchProcessor
{
    private readonly IChatService _chatService;

    public FileBatchProcessor(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task ProcessPromptsFromFileAsync(
        string inputFile,
        string outputFile,
        CancellationToken cancellationToken = default)
    {
        // Read prompts from file (one per line)
        var prompts = await File.ReadAllLinesAsync(inputFile, cancellationToken);

        // Process prompts
        var results = new List<string>();
        foreach (var prompt in prompts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = new ChatCompletionRequest
            {
                Model = "gpt-4",
                Messages = new[]
                {
                    new ChatMessage { Role = "user", Content = prompt }
                }
            };

            var response = await _chatService.CompleteAsync(request, cancellationToken);
            results.Add(response.Choices[0].Message.Content);
        }

        // Write results to file
        await File.WriteAllLinesAsync(outputFile, results, cancellationToken);
    }
}

// Usage
var fileProcessor = app.Services.GetRequiredService<FileBatchProcessor>();
await fileProcessor.ProcessPromptsFromFileAsync("prompts.txt", "results.txt");
```

### Process JSON Batch File

```csharp
public class JsonBatchProcessor
{
    private readonly IChatService _chatService;

    public JsonBatchProcessor(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task ProcessJsonBatchAsync(
        string inputFile,
        string outputFile,
        CancellationToken cancellationToken = default)
    {
        // Read JSON file
        var json = await File.ReadAllTextAsync(inputFile, cancellationToken);
        var batchData = JsonSerializer.Deserialize<List<BatchItem>>(json);

        // Process items
        var results = new List<BatchResultItem>();
        foreach (var item in batchData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var request = new ChatCompletionRequest
                {
                    Model = item.Model ?? "gpt-4",
                    Messages = item.Messages.Select(m =>
                        new ChatMessage { Role = m.Role, Content = m.Content }
                    ).ToArray()
                };

                var response = await _chatService.CompleteAsync(request, cancellationToken);

                results.Add(new BatchResultItem
                {
                    Id = item.Id,
                    Status = "completed",
                    Response = response.Choices[0].Message.Content
                });
            }
            catch (Exception ex)
            {
                results.Add(new BatchResultItem
                {
                    Id = item.Id,
                    Status = "failed",
                    Error = ex.Message
                });
            }
        }

        // Write results to JSON file
        var outputJson = JsonSerializer.Serialize(results, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(outputFile, outputJson, cancellationToken);
    }
}

// Data models
public record BatchItem(
    string Id,
    string Model,
    List<MessageData> Messages
);

public record MessageData(
    string Role,
    string Content
);

public record BatchResultItem(
    string Id,
    string Status,
    string Response = null,
    string Error = null
);

// Usage
var jsonProcessor = app.Services.GetRequiredService<JsonBatchProcessor>();
await jsonProcessor.ProcessJsonBatchAsync("batch-input.json", "batch-output.json");
```

## Tips and Best Practices

1. **Use concurrency** for independent requests
2. **Implement rate limiting** to avoid API throttling
3. **Add retry logic** for transient failures
4. **Track progress** for long-running batches
5. **Handle errors gracefully** and continue processing
6. **Use cancellation tokens** for long-running operations
7. **Cache results** to avoid duplicate processing

## Next Steps

- [Simple Chat Example](./simple-chat-curl.md) - Basic chat completion
- [Streaming Example](./streaming-javascript.md) - Streaming responses
- [Multi-Modal Example](./multimodal-python.md) - Work with images and audio

## Support

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **GitHub Issues**: [https://github.com/rudironsoni/Synaxis/issues](https://github.com/rudironsoni/Synaxis/issues)
- **Discord**: [Join our Discord](https://discord.gg/synaxis)

// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// Create HTTP client with API key authentication
using var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.synaxis.io"), // Replace with your Synaxis endpoint
    Timeout = TimeSpan.FromMinutes(2)
};

// Set API key from environment variable or configuration
var apiKey = Environment.GetEnvironmentVariable("SYNAXIS_API_KEY") ?? "your-api-key-here";
httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

Console.WriteLine("Synaxis SaaS Client Sample");
Console.WriteLine("===========================\n");

// Example 1: Simple chat request
Console.WriteLine("Example 1: Simple Chat Request");
Console.WriteLine("--------------------------------");
await SimpleChatExample(httpClient);

Console.WriteLine("\n");

// Example 2: Streaming chat request
Console.WriteLine("Example 2: Streaming Chat Request");
Console.WriteLine("----------------------------------");
await StreamingChatExample(httpClient);

Console.WriteLine("\n");

// Example 3: Error handling
Console.WriteLine("Example 3: Error Handling");
Console.WriteLine("-------------------------");
await ErrorHandlingExample(httpClient);

Console.WriteLine("\n");

// Example 4: Retry logic
Console.WriteLine("Example 4: Retry Logic");
Console.WriteLine("----------------------");
await RetryLogicExample(httpClient);

Console.WriteLine("\nAll examples completed!");

/// <summary>
/// Demonstrates a simple chat request with basic error handling.
/// </summary>
static async Task SimpleChatExample(HttpClient client)
{
    try
    {
        var request = new ChatRequest
        {
            Messages = new[]
            {
                new ChatMessage { Role = "user", Content = "What is Synaxis?" }
            },
            Model = "gpt-3.5-turbo",
            Temperature = 0.7,
            MaxTokens = 500
        };

        var response = await client.PostAsJsonAsync("/v1/chat/completions", request);
        response.EnsureSuccessStatusCode();

        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
        
        if (chatResponse?.Choices?.Length > 0)
        {
            Console.WriteLine($"Response: {chatResponse.Choices[0].Message?.Content}");
            Console.WriteLine($"Tokens: {chatResponse.Usage?.TotalTokens ?? 0}");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"HTTP Error: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

/// <summary>
/// Demonstrates streaming chat responses for real-time output.
/// </summary>
static async Task StreamingChatExample(HttpClient client)
{
    try
    {
        var request = new ChatRequest
        {
            Messages = new[]
            {
                new ChatMessage { Role = "user", Content = "Count from 1 to 10." }
            },
            Model = "gpt-3.5-turbo",
            Stream = true
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        Console.Write("Streaming response: ");
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
            {
                continue;
            }

            var data = line.Substring(6); // Remove "data: " prefix
            
            if (data == "[DONE]")
            {
                break;
            }

            try
            {
                var chunk = JsonSerializer.Deserialize<ChatStreamChunk>(data);
                if (chunk?.Choices?.Length > 0 && chunk.Choices[0].Delta?.Content != null)
                {
                    Console.Write(chunk.Choices[0].Delta?.Content ?? string.Empty);
                }
            }
            catch (JsonException)
            {
                // Skip malformed JSON
            }
        }

        Console.WriteLine("\n[Stream completed]");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Streaming Error: {ex.Message}");
    }
}

/// <summary>
/// Demonstrates comprehensive error handling for various failure scenarios.
/// </summary>
static async Task ErrorHandlingExample(HttpClient client)
{
    try
    {
        // Intentionally invalid request (empty messages)
        var request = new ChatRequest
        {
            Messages = Array.Empty<ChatMessage>(),
            Model = "gpt-3.5-turbo"
        };

        var response = await client.PostAsJsonAsync("/v1/chat/completions", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API Error ({(int)response.StatusCode}): {errorContent}");
            
            // Try to parse error response
            try
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                Console.WriteLine($"Error Code: {error?.Error?.Code}");
                Console.WriteLine($"Error Message: {error?.Error?.Message}");
            }
            catch
            {
                // Could not parse error response
            }
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Network Error: {ex.Message}");
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("Request timed out");
    }
}

/// <summary>
/// Demonstrates retry logic with exponential backoff for transient failures.
/// </summary>
static async Task RetryLogicExample(HttpClient client)
{
    const int maxRetries = 3;
    const int baseDelayMs = 1000;

    for (int attempt = 0; attempt <= maxRetries; attempt++)
    {
        try
        {
            Console.WriteLine($"Attempt {attempt + 1} of {maxRetries + 1}...");

            var request = new ChatRequest
            {
                Messages = new[]
                {
                    new ChatMessage { Role = "user", Content = "Hello!" }
                },
                Model = "gpt-3.5-turbo"
            };

            var response = await client.PostAsJsonAsync("/v1/chat/completions", request);

            // Retry on 5xx server errors or 429 rate limit
            if ((int)response.StatusCode >= 500 || (int)response.StatusCode == 429)
            {
                if (attempt < maxRetries)
                {
                    var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                    Console.WriteLine($"Server error {(int)response.StatusCode}. Retrying in {delay}ms...");
                    await Task.Delay(delay);
                    continue;
                }
            }

            response.EnsureSuccessStatusCode();
            Console.WriteLine("Request succeeded!");
            break;
        }
        catch (HttpRequestException ex)
        {
            if (attempt < maxRetries)
            {
                var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                Console.WriteLine($"Request failed: {ex.Message}. Retrying in {delay}ms...");
                await Task.Delay(delay);
            }
            else
            {
                Console.WriteLine($"Request failed after {maxRetries + 1} attempts: {ex.Message}");
            }
        }
    }
}

// Data models for API requests and responses

public record ChatRequest
{
    public ChatMessage[] Messages { get; init; } = Array.Empty<ChatMessage>();
    public string Model { get; init; } = "gpt-3.5-turbo";
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public bool Stream { get; init; }
}

public record ChatMessage
{
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public record ChatResponse
{
    public string Id { get; init; } = string.Empty;
    public string Object { get; init; } = string.Empty;
    public long Created { get; init; }
    public string Model { get; init; } = string.Empty;
    public ChatChoice[] Choices { get; init; } = Array.Empty<ChatChoice>();
    public ChatUsage? Usage { get; init; }
}

public record ChatChoice
{
    public int Index { get; init; }
    public ChatMessage? Message { get; init; }
    public string? FinishReason { get; init; }
}

public record ChatUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
}

public record ChatStreamChunk
{
    public string Id { get; init; } = string.Empty;
    public string Object { get; init; } = string.Empty;
    public long Created { get; init; }
    public string Model { get; init; } = string.Empty;
    public ChatStreamChoice[] Choices { get; init; } = Array.Empty<ChatStreamChoice>();
}

public record ChatStreamChoice
{
    public int Index { get; init; }
    public ChatDelta? Delta { get; init; }
    public string? FinishReason { get; init; }
}

public record ChatDelta
{
    public string? Role { get; init; }
    public string? Content { get; init; }
}

public record ErrorResponse
{
    public ErrorDetail? Error { get; init; }
}

public record ErrorDetail
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Type { get; init; }
}

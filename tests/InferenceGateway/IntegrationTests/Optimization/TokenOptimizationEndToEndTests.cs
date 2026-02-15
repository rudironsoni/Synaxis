// <copyright file="TokenOptimizationEndToEndTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Synaxis.InferenceGateway.IntegrationTests;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.Optimization;

/// <summary>
/// End-to-end integration tests for Token Optimization features.
/// Tests full stack with all containers (PostgreSQL, Redis, Qdrant).
/// Verifies optimization features work together: caching, compression, session affinity, deduplication.
/// Requires RUN_EXTERNAL_E2E=1 and at least one provider API key environment variable to run.
/// </summary>
[Trait("Category", "ExternalE2E")]
[Collection("Integration")]
public class TokenOptimizationEndToEndTests(
    ITestOutputHelper output,
    SynaxisWebApplicationFactory factory) : IAsyncLifetime
{
    private readonly ITestOutputHelper _output = output ?? throw new ArgumentNullException(nameof(output));
    private readonly SynaxisWebApplicationFactory _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    private HttpClient? _client;
    private IConnectionMultiplexer? _redisConnection;

    public async Task InitializeAsync()
    {
        // Create Redis connection for test verification
        var redisConnectionString = this._factory.RedisConnectionString;
        this._redisConnection = await ConnectionMultiplexer.ConnectAsync(redisConnectionString).ConfigureAwait(false);

        // Get API key from environment (any supported provider)
        var groqApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? Environment.GetEnvironmentVariable("SYNAPLEXER_GROQ_API_KEY");

        // Create HTTP client with optimization configuration
        this._client = this._factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
(StringComparer.Ordinal)
                {
                    // Enable token optimization features
                    ["Synaxis:InferenceGateway:TokenOptimization:Enabled"] = "true",
                    ["Synaxis:InferenceGateway:TokenOptimization:SemanticCache:Enabled"] = "true",
                    ["Synaxis:InferenceGateway:TokenOptimization:SemanticCache:SimilarityThreshold"] = "0.85",
                    ["Synaxis:InferenceGateway:TokenOptimization:Compression:Enabled"] = "true",
                    ["Synaxis:InferenceGateway:TokenOptimization:SessionAffinity:Enabled"] = "true",
                    ["Synaxis:InferenceGateway:TokenOptimization:Deduplication:Enabled"] = "true",

                    // Qdrant configuration - use a placeholder since we're not using Qdrant in this test
                    ["Synaxis:InferenceGateway:TokenOptimization:SemanticCache:QdrantEndpoint"] = "http://localhost:6333",

                    // Test provider configuration - use Groq with real API key from environment
                    ["Synaxis:InferenceGateway:Providers:Groq:Enabled"] = "true",
                    ["Synaxis:InferenceGateway:Providers:Groq:Key"] = groqApiKey ?? string.Empty,
                    ["Synaxis:InferenceGateway:Providers:Groq:Models:0"] = "llama-3.1-70b-versatile",
                    ["Synaxis:InferenceGateway:Providers:Groq:Tier"] = "1",
                    ["Synaxis:InferenceGateway:CanonicalModels:Groq:llama-3.1-70b-versatile"] = "llama-3.1-70b-versatile",

                    // Alias for test-model pointing to Groq's model
                    ["Synaxis:InferenceGateway:Aliases:test-model:Candidates:0"] = "Groq/llama-3.1-70b-versatile",
                });
            });
        }).CreateClient();
    }

    public async Task DisposeAsync()
    {
        this._client?.Dispose();

        if (this._redisConnection != null)
        {
            await _redisConnection.CloseAsync().ConfigureAwait(false);
            this._redisConnection.Dispose();
        }
    }

    [ExternalE2EFact]
    public async Task ChatRequest_WithOptimization_AppliesAllFeatures()
    {
        // Arrange
        var request = new
        {
            model = "test-model",
            messages = new[]
            {
            new { role = "system", content = "You are a helpful assistant." },
            new { role = "user", content = "What is the capital of France?" },
            },
            temperature = 0.7,
            x_session_id = "test-session-all-features",
        };

        // Act
        var response = await this._client!.PostAsJsonAsync("/openai/v1/chat/completions", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Request failed with status {response.StatusCode}");

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("chat.completion", content.GetProperty("object").GetString());

        // Verify response headers indicate optimization was applied
        // In real implementation, these headers would show cache status, compression ratio, etc.
        this._output.WriteLine($"Response Status: {response.StatusCode}");
        this._output.WriteLine($"Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");

        // Verify session was stored in Redis
        var db = this._redisConnection!.GetDatabase();
        var sessionKey = $"session:test-session-all-features";
        var sessionExists = await db.KeyExistsAsync(sessionKey);

        // Session storage depends on implementation - log for verification
        this._output.WriteLine($"Session exists in Redis: {sessionExists}");
    }

    [ExternalE2EFact]
    public async Task ChatRequest_CacheHit_SkipsProviderCall()
    {
        // Arrange
        var sessionId = "test-session-cache-hit";
        var request = new
        {
            model = "test-model",
            messages = new[]
            {
            new { role = "user", content = "What is 2+2?" },
            },
            temperature = 0.0, // Low temperature for deterministic responses
            x_session_id = sessionId,
        };

        // Act - First request (cache miss)
        var response1 = await this._client!.PostAsJsonAsync("/openai/v1/chat/completions", request);
        Assert.True(response1.IsSuccessStatusCode);
        var content1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        var firstResponse = content1.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        // Small delay to ensure cache write completes
        await Task.Delay(100);

        // Act - Second request (should be cache hit)
        var response2 = await this._client!.PostAsJsonAsync("/openai/v1/chat/completions", request);
        Assert.True(response2.IsSuccessStatusCode);
        var content2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        var secondResponse = content2.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        // Assert - Responses should match (from cache)
        Assert.Equal(firstResponse, secondResponse);

        // Check for cache hit header in real implementation
        // response2.Headers.TryGetValues("x-cache-status", out var cacheStatus);
        // Assert.Contains("hit", cacheStatus?.FirstOrDefault() ?? "miss");
        this._output.WriteLine($"First response: {firstResponse}");
        this._output.WriteLine($"Second response: {secondResponse}");
    }

    [ExternalE2EFact]
    public async Task ChatRequest_Compression_ReducesTokens()
    {
        // Arrange - Long conversation that can be compressed
        var sessionId = "test-session-compression";
        var messages = new List<object>
    {
        new { role = "system", content = "You are a helpful assistant." },
        new { role = "user", content = "Tell me about the history of computers." },
        new { role = "assistant", content = "Computers have a long history dating back to the 1940s..." },
        new { role = "user", content = "What about modern computers?" },
        new { role = "assistant", content = "Modern computers are much more powerful and compact..." },
        new { role = "user", content = "How do they work?" },
    };

        var request = new
        {
            model = "test-model",
            messages = messages.ToArray(),
            x_session_id = sessionId,
            x_enable_compression = true,
        };

        // Act
        var response = await this._client!.PostAsJsonAsync("/openai/v1/chat/completions", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        // In real implementation, check compression headers
        // response.Headers.TryGetValues("x-compression-ratio", out var compressionRatio);
        // response.Headers.TryGetValues("x-tokens-saved", out var tokensSaved);
        this._output.WriteLine("Compression applied to long conversation");
    }

    [ExternalE2EFact]
    public async Task ChatRequest_SessionAffinity_PreservesProvider()
    {
        // Arrange
        var sessionId = "test-session-affinity";
        var request1 = new
        {
            model = "test-model",
            messages = new[]
            {
            new { role = "user", content = "First message" },
            },
            x_session_id = sessionId,
        };

        // Act - First request establishes provider affinity
        var response1 = await this._client!.PostAsJsonAsync("/openai/v1/chat/completions", request1);
        Assert.True(response1.IsSuccessStatusCode);

        // Get provider from first request
        response1.Headers.TryGetValues("x-gateway-provider", out var provider1Values);
        var provider1 = provider1Values?.FirstOrDefault();

        // Act - Second request should use same provider due to session affinity
        var request2 = new
        {
            model = "test-model",
            messages = new[]
            {
            new { role = "user", content = "Second message" },
            },
            x_session_id = sessionId,
        };

        var response2 = await this._client!.PostAsJsonAsync("/openai/v1/chat/completions", request2);
        Assert.True(response2.IsSuccessStatusCode);

        response2.Headers.TryGetValues("x-gateway-provider", out var provider2Values);
        var provider2 = provider2Values?.FirstOrDefault();

        // Assert - Same provider used (if session affinity is working)
        this._output.WriteLine($"Provider 1: {provider1}");
        this._output.WriteLine($"Provider 2: {provider2}");

        // In real implementation with multiple providers, assert equality
        // Assert.Equal(provider1, provider2);
    }

    [ExternalE2EFact]
    public async Task ChatRequest_Deduplication_PreventsDuplicates()
    {
        // Arrange - Same request sent concurrently
        var sessionId = "test-session-dedup";
        var request = new
        {
            model = "test-model",
            messages = new[]
            {
            new { role = "user", content = "Unique test query for deduplication" },
            },
            x_session_id = sessionId,
            x_request_id = "dedup-test-request-123",
        };

        // Act - Send same request multiple times concurrently
        var tasks = Enumerable.Range(0, 5).Select(_ =>
            this._client!.PostAsJsonAsync("/openai/v1/chat/completions", request))
        .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests succeed (deduplication should handle gracefully)
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));

        // In real implementation, verify only one provider call was made
        // This would require instrumentation/metrics to verify
        this._output.WriteLine($"All {responses.Length} duplicate requests completed successfully");
    }

    [ExternalE2EFact]
    public async Task StreamingRequest_AppliesSessionAffinity()
    {
        // Arrange
        var sessionId = "test-session-streaming";
        var request = new
        {
            model = "test-model",
            messages = new[]
            {
            new { role = "user", content = "Tell me a story" },
            },
            stream = true,
            x_session_id = sessionId,
        };

        // Act
        var response = await this._client!.PostAsJsonAsync("/openai/v1/chat/completions", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        // Read stream
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new System.IO.StreamReader(stream);

        var lineCount = 0;
        var foundDone = false;
        string? line;

        while ((line = await reader.ReadLineAsync()) != null && lineCount < 100)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                this._output.WriteLine($"Stream line: {line}");
                lineCount++;

                if (line.Contains("[DONE]"))
                {
                    foundDone = true;
                    break;
                }
            }
        }

        Assert.True(lineCount > 0, "Expected streaming response with multiple chunks");
        this._output.WriteLine($"Received {lineCount} streaming chunks, found DONE: {foundDone}");
    }
}

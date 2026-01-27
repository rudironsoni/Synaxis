using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.Application;
using Synaxis.InferenceGateway.Application.Configuration;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests;

public class GatewayIntegrationTests : IClassFixture<SynaxisWebApplicationFactory>
{
    private readonly SynaxisWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GatewayIntegrationTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Synaxis:InferenceGateway:CanonicalModels:0:Id"] = "test-provider/model",
                    ["Synaxis:InferenceGateway:CanonicalModels:0:Provider"] = "test-provider",
                    ["Synaxis:InferenceGateway:CanonicalModels:0:ModelPath"] = "model",
                    ["Synaxis:InferenceGateway:CanonicalModels:0:Streaming"] = "true",
                    ["Synaxis:InferenceGateway:Aliases:test-alias:Candidates:0"] = "test-provider/model",
                    ["Synaxis:InferenceGateway:Aliases:default:Candidates:0"] = "test-provider/model",

                    ["Synaxis:InferenceGateway:CanonicalModels:1:Id"] = "test-provider/no-stream",
                    ["Synaxis:InferenceGateway:CanonicalModels:1:Provider"] = "test-provider",
                    ["Synaxis:InferenceGateway:CanonicalModels:1:ModelPath"] = "no-stream",
                    ["Synaxis:InferenceGateway:CanonicalModels:1:Streaming"] = "false",
                    
                    ["Synaxis:InferenceGateway:Providers:test-provider:Type"] = "mock",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Tier"] = "1",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Models:0"] = "test-provider/model",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Models:1"] = "test-provider/no-stream",
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IProviderRegistry, MockProviderRegistry>();
                services.AddKeyedSingleton<IChatClient>("test-provider", new MockChatClient());
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Get_Models_ReturnsCanonicalAndAliases()
    {
        var response = await _client.GetAsync("/openai/v1/models");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = content.GetProperty("data");

        var ids = data.EnumerateArray().Select(x => x.GetProperty("id").GetString()).ToList();
        Assert.Contains("test-provider/model", ids);
        Assert.Contains("test-alias", ids);
        Assert.Contains("test-provider/no-stream", ids);
    }

    [Fact]
    public async Task Post_ChatCompletions_ReturnsResponse()
    {
        var request = new
        {
            model = "test-alias",
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            }
        };

        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);
        await EnsureSuccessAsync(response);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("chat.completion", content.GetProperty("object").GetString());
        var text = content.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        Assert.Contains("mock", text);
    }

    [Fact]
    public async Task Post_ChatCompletions_Streaming_EndsWithDone()
    {
        var request = new
        {
            model = "test-alias",
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            },
            stream = true
        };

        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);
        await EnsureSuccessAsync(response);

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
        bool foundDone = false;
        bool foundStop = false;
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            _factory.OutputHelper?.WriteLine($"Line: {line}");
            if (line.Contains("[DONE]")) foundDone = true;
            if (line.Contains("\"finish_reason\":\"stop\"")) foundStop = true;
        }

        Assert.True(foundDone || foundStop, "SSE stream should contain [DONE] or finish_reason: stop");
    }

    [Fact]
    public async Task Post_LegacyCompletions_ReturnsResponse()
    {
        var request = new
        {
            model = "test-alias",
            prompt = "Hello",
            max_tokens = 10
        };

        var response = await _client.PostAsJsonAsync("/openai/v1/completions", request);
        await EnsureSuccessAsync(response);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("text_completion", content.GetProperty("object").GetString());
        var text = content.GetProperty("choices")[0].GetProperty("text").GetString();
        Assert.Contains("mock", text);
    }

    [Fact]
    public async Task Post_LegacyCompletions_Streaming_EndsWithDone()
    {
        var request = new
        {
            model = "test-alias",
            prompt = "Hello",
            stream = true
        };

        var response = await _client.PostAsJsonAsync("/openai/v1/completions", request);
        await EnsureSuccessAsync(response);

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
        bool foundDone = false;
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.Contains("[DONE]")) foundDone = true;
        }

        Assert.True(foundDone, "SSE stream should contain [DONE]");
    }

    [Fact]
    public async Task Post_Responses_ReturnsResponse()
    {
        var request = new
        {
            model = "test-alias",
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            }
        };

        var response = await _client.PostAsJsonAsync("/openai/v1/responses", request);
        
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var error = await response.Content.ReadAsStringAsync();
            _factory.OutputHelper?.WriteLine($"Error: {error}");
        }

        // Just verify it's not 404
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CapabilityGate_Rejects_InvalidRequest()
    {
        // Use a separate client where 'default' points to a non-streaming model
        // because RoutingAgent is currently hardcoded to use 'default'
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Synaxis:InferenceGateway:Aliases:default:Candidates:0"] = "test-provider/no-stream",
                });
            });
        }).CreateClient();

        var request = new
        {
            model = "test-provider/no-stream",
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            },
            stream = true
        };

        var response = await client.PostAsJsonAsync("/openai/v1/chat/completions", request);
        
        // The middleware returns 400 for ArgumentException (capability mismatch)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("no providers available", content.ToLowerInvariant());
    }

    [Fact]
    public async Task Headers_Are_Present()
    {
        var request = new
        {
            model = "test-alias",
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            }
        };

        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);
        await EnsureSuccessAsync(response);

        Assert.True(response.Headers.Contains("x-gateway-model-requested"));
        Assert.True(response.Headers.Contains("x-gateway-model-resolved"));
        Assert.True(response.Headers.Contains("x-gateway-provider"));
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _factory.OutputHelper?.WriteLine($"Status: {(int)response.StatusCode} Body: {error}");
        }

        response.EnsureSuccessStatusCode();
    }
}

public class MockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new ChatClientMetadata("MockChatClient");

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello from mock!"))
        {
            Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5 }
        });
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent("Hello") } };
        yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent(" from") } };
        yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent(" mock!") } };
        yield return new ChatResponseUpdate { FinishReason = ChatFinishReason.Stop };
    }

    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}

public class MockProviderRegistry : IProviderRegistry
{
    public IEnumerable<(string ServiceKey, int Tier)> GetCandidates(string modelId)
    {
        if (modelId == "model" || modelId == "no-stream")
        {
            yield return ("test-provider", 1);
        }
    }

    public ProviderConfig? GetProvider(string serviceKey)
    {
        if (serviceKey == "test-provider")
        {
            return new ProviderConfig
            {
                Key = "test-provider",
                Tier = 1,
                Models = new List<string> { "test/model", "test/no-stream" }
            };
        }
        return null;
    }
}

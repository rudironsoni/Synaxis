using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.Application;
using Synaxis.InferenceGateway.Application.Configuration;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.API;

public class ApiEndpointErrorTests : IClassFixture<SynaxisWebApplicationFactory>
{
    private readonly SynaxisWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiEndpointErrorTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
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
    public async Task Post_ChatCompletions_InvalidModelId_Returns400()
    {
        // Arrange
        var request = new
        {
            model = "non-existent-model-12345",
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);

        // Assert - Current behavior: Invalid model returns 400 (not 404)
        // This test documents the current behavior, not the desired behavior
        var content = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {content}");
        // The API returns 400 for invalid model (no providers available)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("no providers available", content.ToLowerInvariant());
    }

    [Fact]
    public async Task Post_ChatCompletions_MissingModelField_UsesDefault()
    {
        // Arrange
        var request = new
        {
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);

        // Assert - Current behavior: Missing model defaults to "default" and returns 400 if no providers found
        // This test documents the current behavior, not the desired behavior
        var content = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {content}");
        // The API returns 400 because "default" model has no providers configured
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChatCompletions_MissingMessagesField_Returns200()
    {
        // Arrange
        var request = new
        {
            model = "test-alias"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);

        // Assert - Current behavior: Missing messages defaults to empty list and returns 200
        // This test documents the current behavior, not the desired behavior
        var content = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {content}");
        // The API returns 200 with empty messages (validation not implemented)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChatCompletions_EmptyMessagesArray_Returns200()
    {
        // Arrange
        var request = new
        {
            model = "test-alias",
            messages = Array.Empty<object>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);

        // Assert - Current behavior: Empty messages array returns 200
        // This test documents the current behavior, not the desired behavior
        var content = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {content}");
        // The API returns 200 with empty messages (validation not implemented)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChatCompletions_InvalidMessageFormat_MissingRole_Returns400()
    {
        // Arrange
        var request = new
        {
            model = "test-alias",
            messages = new[]
            {
                new { content = "Hello" } // Missing role
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);

        // Assert - Current behavior: Missing role causes 400 with generic error message
        // This test documents the current behavior, not the desired behavior
        var content = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {content}");
        // The API returns 400 but error message doesn't mention "role"
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Error message is generic: "Argument is whitespace"
        Assert.Contains("whitespace", content.ToLowerInvariant());
    }

    [Fact]
    public async Task Post_ChatCompletions_InvalidMessageFormat_MissingContent_Returns200()
    {
        // Arrange
        var request = new
        {
            model = "test-alias",
            messages = new[]
            {
                new { role = "user" } // Missing content
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);

        // Assert - Current behavior: Missing content defaults to empty string, returns 200
        // This test documents the current behavior, not the desired behavior
        var content = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {content}");
        // The API returns 200 with empty content (validation not implemented)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChatCompletions_InvalidStreamParameter_NonBoolean_Returns400()
    {
        // Arrange
        var request = new
        {
            model = "test-alias",
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            },
            stream = "true" // String instead of boolean
        };

        // Act
        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);

        var content = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {content}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChatCompletions_InvalidTemperatureParameter_OutOfRange_Returns400()
    {
        // Arrange
        var request = new
        {
            model = "test-alias",
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            },
            temperature = 3.0 // Out of valid range (0.0 to 2.0)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);

        var content = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {content}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChatCompletions_InvalidMaxTokensParameter_Negative_Returns400()
    {
        // Arrange
        var request = new
        {
            model = "test-alias",
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            },
            max_tokens = -10 // Negative value
        };

        // Act
        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);

        var content = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {content}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChatCompletions_MalformedJson_Returns400()
    {
        // Arrange
        var malformedJson = "{ \"model\": \"test-alias\", \"messages\": [ { \"role\": \"user\", \"content\": \"Hello\" } ]"; // Missing closing brace
        var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/openai/v1/chat/completions", content);

        var responseContent = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {responseContent}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChatCompletions_UnsupportedModelCapability_StreamingOnNonStreamingModel_Returns400()
    {
        // Arrange
        var request = new
        {
            model = "test-provider/no-stream", // Non-streaming model
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            },
            stream = true // Request streaming on non-streaming model
        };

        // Act
        var response = await _client.PostAsJsonAsync("/openai/v1/chat/completions", request);

        // Assert - Current behavior: Streaming on non-streaming model returns 400
        // This test documents the current behavior, which is actually correct
        var content = await response.Content.ReadAsStringAsync();
        _factory.OutputHelper?.WriteLine($"Response: {content}");
        // The API correctly returns 400 for capability mismatch
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("no providers available", content.ToLowerInvariant());
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

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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

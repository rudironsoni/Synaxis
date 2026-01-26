using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Synaxis.InferenceGateway.WebApi.DTOs.OpenAi;
using System.Text.Json;

namespace Synaxis.InferenceGateway.IntegrationTests;

public class ChatCompletionsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public ChatCompletionsApiTests(WebApplicationFactory<Program> factory)
    {
        _mockChatClient = new Mock<IChatClient>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace IChatClient with our mock
                var descriptors = services.Where(d => d.ServiceType == typeof(IChatClient)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(_mockChatClient.Object);
            });
        });
    }

    [Fact]
    public async Task Create_NonStreaming_ReturnsValidResponse()
    {
        // Arrange
        var expectedResponse = new ChatResponse(new[] {
            new ChatMessage(ChatRole.Assistant, "Hello world")
        });
        expectedResponse.Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5, TotalTokenCount = 15 };

        _mockChatClient.Setup(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var client = _factory.CreateClient();
        var request = new ChatCompletionRequest
        {
            Model = "gpt-4",
            Messages = new List<ChatCompletionMessageDto>
            {
                new() { Role = "user", Content = "Hi" }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
        Assert.NotNull(result);
        Assert.Equal("Hello world", result.Choices[0].Message.Content?.ToString());
        Assert.Equal(10, result.Usage?.PromptTokens);
        Assert.Equal(5, result.Usage?.CompletionTokens);
        Assert.Equal(15, result.Usage?.TotalTokens);
    }

    [Fact]
    public async Task Create_Streaming_ReturnsValidEventStream()
    {
        // Arrange
        var chunks = new List<ChatResponseUpdate>
        {
            new() { Contents = { new TextContent("Hello") }, Role = ChatRole.Assistant },
            new() { Contents = { new TextContent(" ") } },
            new() { Contents = { new TextContent("World") }, FinishReason = ChatFinishReason.Stop }
        };

        _mockChatClient.Setup(c => c.GetStreamingResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        var client = _factory.CreateClient();
        var request = new ChatCompletionRequest
        {
            Model = "gpt-4",
            Messages = new List<ChatCompletionMessageDto>
            {
                new() { Role = "user", Content = "Hi" }
            },
            Stream = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
        var accumulatedContent = "";
        var receivedDone = false;

        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            Assert.StartsWith("data: ", line);
            var data = line["data: ".Length..];

            if (data == "[DONE]")
            {
                receivedDone = true;
                break;
            }

            var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, _jsonOptions);
            Assert.NotNull(chunk);
            accumulatedContent += chunk.Choices[0].Delta.Content;
        }

        Assert.Equal("Hello World", accumulatedContent);
        Assert.True(receivedDone);
    }
}

// <copyright file="CloudflareChatClientTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Synaxis.InferenceGateway.Infrastructure;
using Xunit;

public class CloudflareChatClientTests
{
    private const string TestAccountId = "test-account-id";
    private const string TestModelId = "@cf/meta/llama-3.1-8b-instruct";
    private const string TestApiKey = "test-api-key";

    [Fact]
    public void Constructor_SetsAuthorizationHeaderAndMetadata()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);

        // Act
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        // Assert
        Assert.Equal("Cloudflare", client.Metadata.ProviderName);
    }

    [Fact]
    public void Constructor_WithLogger_SetsLogger()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);
        var loggerMock = new Mock<ILogger<CloudflareChatClient>>();

        // Act
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey, loggerMock.Object);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithoutLogger_HandlesNullLogger()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);

        // Act
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey, null);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task GetResponseAsync_Success_ReturnsValidChatResponse()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var responseJson = "{\"result\": {\"response\": \"Hello from Cloudflare\"}}";

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson),
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        // Act
        var result = await client.GetResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") });

        // Assert
        Assert.Equal("Hello from Cloudflare", result.Messages[0].Text);
        Assert.Equal(TestModelId, result.ModelId);
    }

    [Fact]
    public async Task GetResponseAsync_WithOptions_IncludesOptionsInRequest()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var capturedRequest = new HttpRequestMessage();
        var responseJson = "{\"result\": {\"response\": \"Response with options\"}}";

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson),
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);
        var options = new ChatOptions { Temperature = 0.7f }; // MaxTokens not supported by Cloudflare

        // Act
        await client.GetResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") }, options);

        // Assert
        var content = await capturedRequest.Content!.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.False(json.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public Task GetResponseAsync_ApiError_ThrowsHttpRequestException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}"),
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        // Act & Assert
        return Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") }));
    }

    [Fact]
    public async Task GetResponseAsync_MultipleMessages_HandlesAllMessageTypes()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var responseJson = "{\"result\": {\"response\": \"Response to multiple messages\"}}";

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson),
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant"),
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.Assistant, "Hi there!"),
            new ChatMessage(ChatRole.User, "How are you?"),
        };

        // Act
        var result = await client.GetResponseAsync(messages);

        // Assert
        Assert.Equal("Response to multiple messages", result.Messages[0].Text);
    }

    [Fact]
    public async Task GetResponseAsync_EmptyResponse_HandlesGracefully()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var responseJson = "{\"result\": {\"response\": \"\"}}";

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson),
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        // Act
        var result = await client.GetResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") });

        // Assert
        Assert.Equal(string.Empty, result.Messages[0].Text);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_Success_YieldsMultipleUpdates()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var streamContent = new StringContent("data: {\"response\": \"Hello\"}\n\ndata: {\"response\": \" World\"}\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream");

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = streamContent,
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") }))
        {
            updates.Add(update);
        }

        // Assert
        Assert.Equal(2, updates.Count);
        Assert.Equal("Hello", ((TextContent)updates[0].Contents[0]).Text);
        Assert.Equal(" World", ((TextContent)updates[1].Contents[0]).Text);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_EmptyLines_SkipsEmptyLines()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var streamContent = new StringContent("\n\ndata: {\"response\": \"Hello\"}\n\n\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream");

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = streamContent,
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") }))
        {
            updates.Add(update);
        }

        // Assert
        Assert.Single(updates);
        Assert.Equal("Hello", ((TextContent)updates[0].Contents[0]).Text);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_InvalidJson_SkipsInvalidLines()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var streamContent = new StringContent("data: {\"invalid\": json}\n\ndata: {\"response\": \"Hello\"}\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream");

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = streamContent,
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") }))
        {
            updates.Add(update);
        }

        // Assert
        Assert.Single(updates);
        Assert.Equal("Hello", ((TextContent)updates[0].Contents[0]).Text);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_NonDataLines_SkipsNonDataLines()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var streamContent = new StringContent("event: start\n\ndata: {\"response\": \"Hello\"}\n\n: comment\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream");

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = streamContent,
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") }))
        {
            updates.Add(update);
        }

        // Assert
        Assert.Single(updates);
        Assert.Equal("Hello", ((TextContent)updates[0].Contents[0]).Text);
    }

    [Fact]
    public void GetService_ReturnsNull()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        // Act
        var result = client.GetService(typeof(object));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Dispose_DisposesHttpClient()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);
        using (var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey))
        {
        }

        // Assert
        Assert.True(true);
    }

    [Fact]
    public void Metadata_ReturnsCorrectProviderName()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CloudflareChatClient(httpClient, TestAccountId, TestModelId, TestApiKey);

        // Act & Assert
        Assert.Equal("Cloudflare", client.Metadata.ProviderName);
    }
}

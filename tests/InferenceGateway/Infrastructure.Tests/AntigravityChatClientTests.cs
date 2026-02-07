using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Moq;
using Moq.Protected;
using Synaxis.InferenceGateway.Infrastructure;
using Synaxis.InferenceGateway.Infrastructure.Auth;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests;

public class AntigravityChatClientTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly Mock<ITokenProvider> _tokenProviderMock;
    private readonly string _modelId = "gemini-3-pro-high";
    private readonly string _projectId = "test-project";
    private readonly string _fakeToken = "fake-auth-token";

    public AntigravityChatClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _tokenProviderMock = new Mock<ITokenProvider>();
        _tokenProviderMock.Setup(x => x.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fakeToken);
    }

    [Fact]
    public async Task GetResponseAsync_SendsCorrectRequest_ReturnsResponse()
    {
        // Arrange
        var responseJson = @"{
            ""response"": {
                ""candidates"": [
                    {
                        ""content"": {
                            ""role"": ""model"",
                            ""parts"": [{ ""text"": ""Hello form Antigravity"" }]
                        },
                        ""finishReason"": ""STOP""
                    }
                ],
                ""modelVersion"": ""gemini-3-pro-high"",
                ""responseId"": ""req-123""
            }
        }";

        HttpRequestMessage? capturedRequest = null;
        string? requestBody = null;

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedRequest = req;
                requestBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://cloudcode-pa.googleapis.com")
        };
        var client = new AntigravityChatClient(httpClient, _modelId, _projectId, _tokenProviderMock.Object);
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "Be helpful"),
            new ChatMessage(ChatRole.User, "Hi")
        };

        // Act
        var result = await client.GetResponseAsync(messages);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.Equal("Hello form Antigravity", result.Messages[0].Text);
        Assert.Equal("req-123", result.ResponseId);

        // Verify Request
        Assert.NotNull(capturedRequest);
        Assert.Equal("https://cloudcode-pa.googleapis.com/v1/chat/completions", capturedRequest!.RequestUri?.ToString());
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal(_fakeToken, capturedRequest.Headers.Authorization?.Parameter);
        Assert.Contains("antigravity/1.11.5", capturedRequest.Headers.UserAgent.ToString());

        Assert.NotNull(requestBody);
        var doc = JsonDocument.Parse(requestBody);
        var root = doc.RootElement;

        Assert.Equal(_projectId, root.GetProperty("project").GetString());
        Assert.Equal(_modelId, root.GetProperty("model").GetString());

        var req = root.GetProperty("request");
        Assert.Equal("Be helpful", req.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Equal("Hi", req.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task GetStreamingResponseAsync_ParsesSSE_YieldsUpdates()
    {
        // Arrange
        var streamContent =
@"data: { ""response"": { ""candidates"": [{ ""content"": { ""role"": ""model"", ""parts"": [{ ""text"": ""Hello"" }] }, ""responseId"": ""1"" }] } }

data: { ""response"": { ""candidates"": [{ ""content"": { ""role"": ""model"", ""parts"": [{ ""text"": "" World"" }] }, ""responseId"": ""1"" }] } }

data: [DONE]
";
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(streamContent)
            });

        var httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://cloudcode-pa.googleapis.com")
        };
        var client = new AntigravityChatClient(httpClient, _modelId, _projectId, _tokenProviderMock.Object);

        // Act
        var parts = new List<string>();
        await foreach (var update in client.GetStreamingResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") }))
        {
            parts.Add(update.Text);
        }

        // Assert
        Assert.Equal(2, parts.Count);
        Assert.Equal("Hello", parts[0]);
        Assert.Equal(" World", parts[1]);
    }
}

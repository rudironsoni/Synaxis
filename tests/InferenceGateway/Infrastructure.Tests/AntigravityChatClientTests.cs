// <copyright file="AntigravityChatClientTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests;

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

public class AntigravityChatClientTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly Mock<ITokenProvider> _tokenProviderMock;
    private readonly string _modelId = "gemini-3-pro-high";
    private readonly string _projectId = "test-project";
    private readonly string _fakeToken = "fake-auth-token";

    public AntigravityChatClientTests()
    {
        this._handlerMock = new Mock<HttpMessageHandler>();
        this._tokenProviderMock = new Mock<ITokenProvider>();
        this._tokenProviderMock.Setup(x => x.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(this._fakeToken);
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

        this._handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                capturedRequest = req;
                requestBody = req.Content != null ? req.Content.ReadAsStringAsync().GetAwaiter().GetResult() : null;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson),
                };
            });

        using var httpClient = new HttpClient(this._handlerMock.Object)
        {
            BaseAddress = new Uri("https://cloudcode-pa.googleapis.com"),
        };
        var client = new AntigravityChatClient(httpClient, this._modelId, this._projectId, this._tokenProviderMock.Object);
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "Be helpful"),
            new ChatMessage(ChatRole.User, "Hi"),
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
        Assert.Equal(this._fakeToken, capturedRequest.Headers.Authorization?.Parameter);
        Assert.Contains("antigravity/1.11.5", capturedRequest.Headers.UserAgent.ToString(), StringComparison.Ordinal);

        Assert.NotNull(requestBody);
        var doc = JsonDocument.Parse(requestBody);
        var root = doc.RootElement;

        Assert.Equal(this._projectId, root.GetProperty("project").GetString());
        Assert.Equal(this._modelId, root.GetProperty("model").GetString());

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
        this._handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(streamContent),
            });

        using var httpClient = new HttpClient(this._handlerMock.Object)
        {
            BaseAddress = new Uri("https://cloudcode-pa.googleapis.com"),
        };
        var client = new AntigravityChatClient(httpClient, this._modelId, this._projectId, this._tokenProviderMock.Object);

        // Act
        var parts = new List<string>();
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "Hi")]))
        {
            parts.Add(update.Text);
        }

        // Assert
        Assert.Equal(2, parts.Count);
        Assert.Equal("Hello", parts[0]);
        Assert.Equal(" World", parts[1]);
    }
}

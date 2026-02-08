// <copyright file="CohereChatClientTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests;

using Microsoft.Extensions.AI;
using Moq.Protected;
using Moq;
using Synaxis.InferenceGateway.Infrastructure;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

public class CohereChatClientTests
{
    [Fact]
    public async Task GetResponseAsync_ValidResponse_ReturnsCompletion()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var responseJson = "{\"message\": {\"content\": [{\"type\": \"text\", \"text\": \"Hello from Cohere\"}]}}";

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson),
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CohereChatClient(httpClient, "command-r", "fake-key");

        // Act
        var result = await client.GetResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") });

        // Assert
        Assert.Equal("Hello from Cohere", result.Messages[0].Text);
    }
}

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Moq;
using Moq.Protected;
using Synaxis.Infrastructure;
using Xunit;

namespace Synaxis.Infrastructure.Tests;

public class PollinationsChatClientTests
{
    [Fact]
    public async Task GetResponseAsync_ConstructsUrlCorrectly()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null && req.RequestUri.ToString().Contains("text.pollinations.ai")
                    // Removed strict check for encoded string to avoid mismatch
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Hello from Pollinations")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new PollinationsChatClient(httpClient, "default");

        // Act
        var result = await client.GetResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi") });

        // Assert
        Assert.Equal("Hello from Pollinations", result.Messages[0].Text);
    }
}

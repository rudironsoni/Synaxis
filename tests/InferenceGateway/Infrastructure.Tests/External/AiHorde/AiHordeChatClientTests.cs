
namespace Synaxis.InferenceGateway.Infrastructure.External.AiHorde.Tests;

using Microsoft.Extensions.AI;
using Moq.Protected;
using Moq;
using System.Net.Http.Json;
using System.Net.Http;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

public class AiHordeChatClientTests
{
    [Fact]
    public async Task GetResponseAsync_ReturnsFinalText()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();

        // First POST returns id
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Post), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { id = "abc123" }),
            });

        // Then GET returns done=false once, then done=true
        var sequence = 0;
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                sequence++;
                if (sequence == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new { done = false }),
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { done = true, text = "Hello" }),
                };
            });

        var client = new HttpClient(handler.Object);
        var ai = new AiHordeChatClient(client);

        var resp = await ai.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") }).ConfigureAwait(false);

        Assert.Single(resp.Messages);
        Assert.Equal("Hello", resp.Messages[0].Text);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_YieldsFinal()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Post), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { id = "xyz" }),
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { done = true, text = "StreamingText" }),
            });

        var client = new HttpClient(handler.Object);
        var ai = new AiHordeChatClient(client);

        var stream = ai.GetStreamingResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") });
        var enumerator = stream.GetAsyncEnumerator();
        try
        {
            Assert.True(await enumerator.MoveNextAsync()).ConfigureAwait(false);
            var update = enumerator.Current;
            Assert.Single(update.Contents);
            Assert.Equal("StreamingText", ((TextContent)update.Contents[0]).Text);
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }
}

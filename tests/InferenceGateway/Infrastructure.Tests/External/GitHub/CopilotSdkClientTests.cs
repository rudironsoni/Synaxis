using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.External.GitHub;

public class CopilotSdkClientTests
{
    [Fact]
    public async Task GetResponseAsync_DelegatesToAdapter()
    {
        var adapterMock = new Mock<Synaxis.InferenceGateway.Infrastructure.External.GitHub.ICopilotSdkAdapter>();
        var expected = new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"));
        adapterMock.Setup(a => a.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var client = new Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkClient(adapterMock.Object);

        var res = await client.GetResponseAsync(new List<ChatMessage> { new ChatMessage(ChatRole.User, "hi") });

        Assert.Equal("ok", res.Messages.First().Text);
        adapterMock.Verify(a => a.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_StreamsFromAdapter()
    {
        var adapterMock = new Mock<Synaxis.InferenceGateway.Infrastructure.External.GitHub.ICopilotSdkAdapter>();

        async IAsyncEnumerable<ChatResponseUpdate> GetUpdates()
        {
            yield return new ChatResponseUpdate { Role = ChatRole.Assistant };
            await Task.Delay(1);
            yield return new ChatResponseUpdate { Role = ChatRole.Assistant };
        }

        adapterMock.Setup(a => a.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(GetUpdates());

        var client = new Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkClient(adapterMock.Object);

        var list = new List<ChatResponseUpdate>();
        await foreach (var u in client.GetStreamingResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") }))
        {
            list.Add(u);
        }

        Assert.Equal(2, list.Count);
        adapterMock.Verify(a => a.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Dispose_DisposesAdapter()
    {
        var adapterMock = new Mock<Synaxis.InferenceGateway.Infrastructure.External.GitHub.ICopilotSdkAdapter>();
        adapterMock.Setup(a => a.Dispose());

        var client = new Synaxis.InferenceGateway.Infrastructure.External.GitHub.CopilotSdkClient(adapterMock.Object);
        client.Dispose();

        adapterMock.Verify(a => a.Dispose(), Times.Once);
    }
}

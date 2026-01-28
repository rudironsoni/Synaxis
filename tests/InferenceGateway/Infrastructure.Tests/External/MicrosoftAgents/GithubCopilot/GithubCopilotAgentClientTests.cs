using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.External.MicrosoftAgents.GithubCopilot;

public class GithubCopilotAgentClientTests
{
    // Helper types that mimic the shape the client inspects via reflection
    private class AgentRunResult
    {
        public IEnumerable<ChatMessage>? Messages { get; set; }
    }

    private class AgentUpdate
    {
        public ChatRole Role { get; set; }
        public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
        public string? FinishReason { get; set; }
        public IEnumerable<object>? Contents { get; set; }
    }

    [Fact]
    public async Task GetResponseAsync_MapsAgentMessagesToChatResponse()
    {
        var agentMock = new Mock<Microsoft.Agents.AI.AIAgent>();

        var msgs = new List<ChatMessage> { new ChatMessage(ChatRole.Assistant, "hello") };
        var runResult = new AgentRunResult { Messages = msgs };

        agentMock.Setup(a => a.RunAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(runResult);

        var client = new Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot.GithubCopilotAgentClient(agentMock.Object, NullLogger<Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot.GithubCopilotAgentClient>.Instance);

        var res = await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") });

        Assert.Single(res.Messages);
        Assert.Equal("hello", res.Messages.First().Text);
        Assert.Equal("copilot", res.ModelId);
        Assert.NotNull(res.AdditionalProperties);
        Assert.Equal("GitHubCopilot", res.AdditionalProperties["provider_name"]);

        agentMock.Verify(a => a.RunAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_FallsBackToToStringWhenNoMessages()
    {
        var agentMock = new Mock<Microsoft.Agents.AI.AIAgent>();
        var sentinel = new { Value = "some result" };
        agentMock.Setup(a => a.RunAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object?)sentinel);

        var client = new Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot.GithubCopilotAgentClient(agentMock.Object);

        var res = await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") });

        Assert.Single(res.Messages);
        Assert.Contains("Value = some result", res.Messages.First().Text);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_StreamsAndMapsAgentUpdates()
    {
        var agentMock = new Mock<Microsoft.Agents.AI.AIAgent>();

        async IAsyncEnumerable<object> GetUpdates()
        {
            yield return new AgentUpdate
            {
                Role = ChatRole.Assistant,
                Contents = new object[] { new TextContent("part1") },
                AdditionalProperties = new AdditionalPropertiesDictionary { ["k"] = "v" },
                FinishReason = "Stop"
            };

            await Task.Delay(1);

            yield return new AgentUpdate
            {
                Role = ChatRole.Assistant,
                Contents = new object[] { "rawtext" }
            };
        }

        agentMock.Setup(a => a.RunStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(GetUpdates());

        var client = new Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot.GithubCopilotAgentClient(agentMock.Object);

        var list = new List<ChatResponseUpdate>();
        await foreach (var u in client.GetStreamingResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") }))
        {
            list.Add(u);
        }

        Assert.Equal(2, list.Count);

        // first update
        Assert.Equal(ChatRole.Assistant, list[0].Role);
        Assert.Single(list[0].Contents);
        Assert.IsType<TextContent>(list[0].Contents.First());
        Assert.Equal("part1", ((TextContent)list[0].Contents.First()).Text);
        Assert.Equal("v", list[0].AdditionalProperties?["k"]);
        Assert.Equal(ChatFinishReason.Stop, list[0].FinishReason);

        // second update: raw string turned into TextContent
        Assert.Equal("rawtext", ((TextContent)list[1].Contents.First()).Text);

        agentMock.Verify(a => a.RunStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetService_ReturnsAgentInstanceWhenTypesMatch()
    {
        var agentMock = new Mock<Microsoft.Agents.AI.AIAgent>();
        var client = new Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot.GithubCopilotAgentClient(agentMock.Object);

        var svc = client.GetService(typeof(Microsoft.Agents.AI.AIAgent));

        Assert.Same(agentMock.Object, svc);
    }

    [Fact]
    public void Dispose_DisposesAgentIfDisposable()
    {
        var agentMock = new Mock<Microsoft.Agents.AI.AIAgent>();
        agentMock.As<IDisposable>().Setup(d => d.Dispose());

        var client = new Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot.GithubCopilotAgentClient(agentMock.Object);
        client.Dispose();

        agentMock.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
    }
}

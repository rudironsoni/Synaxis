using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Copilot.Sdk;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.External.GitHub
{
    public class GitHubCopilotChatClientTests
    {
        [Fact]
        public async Task GetResponseAsync_CreatesSessionSendsAndReturnsResponse()
        {
            var copilotMock = new Mock<CopilotClient>();
            copilotMock.SetupGet(c => c.State).Returns(ConnectionState.Connected);

            var sessionMock = new Mock<CopilotSession>();

            GitHub.Copilot.SDK.SessionEventHandler? registered = null;
            sessionMock.Setup(s => s.On(It.IsAny<GitHub.Copilot.SDK.SessionEventHandler>()))
                .Returns<GitHub.Copilot.SDK.SessionEventHandler>(handler =>
                {
                    registered = handler;
                    return new DisposableAction();
                });

            sessionMock.Setup(s => s.SendAsync(It.IsAny<MessageOptions>(), It.IsAny<CancellationToken>()))
                .Returns<MessageOptions, CancellationToken>((mo, ct) =>
                {
                    // simulate an assistant message event followed by session idle to finish
                    var evt = CreateEvent("AssistantMessageEvent", new Dictionary<string, object?> { ["Content"] = "hello" });
                    registered?.Invoke(evt);
                    var idle = CreateEvent("SessionIdleEvent", new Dictionary<string, object?>());
                    registered?.Invoke(idle);
                    return Task.CompletedTask;
                });

            copilotMock.Setup(c => c.CreateSessionAsync(It.IsAny<SessionConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessionMock.Object);

            var client = new Synaxis.InferenceGateway.Infrastructure.External.GitHub.GitHubCopilotChatClient(copilotMock.Object, NullLogger<Synaxis.InferenceGateway.Infrastructure.External.GitHub.GitHubCopilotChatClient>.Instance);

            var resp = await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") });

            Assert.Contains("hello", resp.Messages.First().Text);
            copilotMock.Verify(c => c.CreateSessionAsync(It.IsAny<SessionConfig>(), It.IsAny<CancellationToken>()), Times.Once);
            sessionMock.Verify(s => s.SendAsync(It.IsAny<MessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetStreamingResponseAsync_StreamsUpdates()
        {
            var copilotMock = new Mock<CopilotClient>();
            copilotMock.SetupGet(c => c.State).Returns(ConnectionState.Connected);

            var sessionMock = new Mock<CopilotSession>();

            GitHub.Copilot.SDK.SessionEventHandler? registered = null;
            sessionMock.Setup(s => s.On(It.IsAny<GitHub.Copilot.SDK.SessionEventHandler>()))
                .Returns<GitHub.Copilot.SDK.SessionEventHandler>(handler =>
                {
                    registered = handler;
                    return new DisposableAction();
                });

            sessionMock.Setup(s => s.SendAsync(It.IsAny<MessageOptions>(), It.IsAny<CancellationToken>()))
                .Returns<MessageOptions, CancellationToken>((mo, ct) =>
                {
                    // send a delta, a full assistant message, a usage event and idle
                    registered?.Invoke(CreateEvent("AssistantMessageDeltaEvent", new Dictionary<string, object?> { ["DeltaContent"] = "d1" }));
                    registered?.Invoke(CreateEvent("AssistantMessageEvent", new Dictionary<string, object?> { ["Content"] = "m1" }));
                    registered?.Invoke(CreateEvent("AssistantUsageEvent", new Dictionary<string, object?> { ["InputTokens"] = 1, ["OutputTokens"] = 2 }));
                    registered?.Invoke(CreateEvent("SessionIdleEvent", new Dictionary<string, object?>()));
                    return Task.CompletedTask;
                });

            copilotMock.Setup(c => c.CreateSessionAsync(It.IsAny<SessionConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessionMock.Object);

            var client = new Synaxis.InferenceGateway.Infrastructure.External.GitHub.GitHubCopilotChatClient(copilotMock.Object, NullLogger<Synaxis.InferenceGateway.Infrastructure.External.GitHub.GitHubCopilotChatClient>.Instance);

            var list = new List<ChatResponseUpdate>();
            await foreach (var u in client.GetStreamingResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") }))
            {
                list.Add(u);
            }

            // Expect 4 updates: delta, assistant message, usage text, and empty on idle (may be empty string)
            Assert.True(list.Count >= 3);
            Assert.Contains(list, u => u.Contents.Any(c => c is TextContent tc && tc.Text.Contains("d1")));
            Assert.Contains(list, u => u.Contents.Any(c => c is TextContent tc && tc.Text.Contains("m1")));
            Assert.Contains(list, u => u.Contents.Any(c => c is TextContent tc && tc.Text.Contains("usage:")));

            copilotMock.Verify(c => c.CreateSessionAsync(It.IsAny<SessionConfig>(), It.IsAny<CancellationToken>()), Times.Once);
            sessionMock.Verify(s => s.SendAsync(It.IsAny<MessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Dispose_DisposesClient()
        {
            var copilotMock = new Mock<CopilotClient>();

            copilotMock.Setup(c => c.DisposeAsync()).Verifiable();

            var client = new Synaxis.InferenceGateway.Infrastructure.External.GitHub.GitHubCopilotChatClient(copilotMock.Object);
            client.Dispose();

            copilotMock.Verify(c => c.DisposeAsync(), Times.Once);
        }

        private class DisposableAction : IDisposable
        {
            public void Dispose() { }
        }

        private static object CreateEvent(string typeName, Dictionary<string, object?> data)
        {
            var asm = typeof(CopilotClient).Assembly;
            var fullName = "GitHub.Copilot.SDK." + typeName;
            var t = asm.GetType(fullName) ?? throw new InvalidOperationException("Type not found: " + fullName);
            var evt = Activator.CreateInstance(t) ?? throw new InvalidOperationException("Could not create event " + fullName);

            // Set Data property if present
            var dataProp = t.GetProperty("Data");
            if (dataProp != null)
            {
                var dataType = dataProp.PropertyType;
                var dataInstance = Activator.CreateInstance(dataType) ?? throw new InvalidOperationException("Could not create data for " + fullName);
                foreach (var kv in data)
                {
                    var p = dataType.GetProperty(kv.Key);
                    if (p != null && p.CanWrite)
                    {
                        p.SetValue(dataInstance, kv.Value);
                    }
                }
                dataProp.SetValue(evt, dataInstance);
            }

            return evt;
        }
    }
}

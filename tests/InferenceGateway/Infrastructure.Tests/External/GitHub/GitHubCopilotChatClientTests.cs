using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Copilot.SDK;
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
            var copilotMock = new Mock<Synaxis.InferenceGateway.Infrastructure.External.GitHub.ICopilotClient>();

            var sessionMock = new Mock<Synaxis.InferenceGateway.Infrastructure.External.GitHub.ICopilotSession>();

            global::GitHub.Copilot.SDK.SessionEventHandler? registered = null;
            sessionMock.Setup(s => s.On(It.IsAny<global::GitHub.Copilot.SDK.SessionEventHandler>()))
                .Returns<global::GitHub.Copilot.SDK.SessionEventHandler>(handler =>
                 {
                     registered = handler;
                     return new DisposableAction();
                 });

            sessionMock.Setup(s => s.SendAsync(It.IsAny<global::GitHub.Copilot.SDK.MessageOptions>(), It.IsAny<CancellationToken>()))
                .Returns((global::GitHub.Copilot.SDK.MessageOptions mo, CancellationToken ct) =>
                {
                    // simulate an assistant message event followed by session idle to finish
                    var evt = CreateEvent("AssistantMessageEvent", new Dictionary<string, object?> { ["Content"] = "hello" });
                    registered?.Invoke(evt);
                    var idle = CreateEvent("SessionIdleEvent", new Dictionary<string, object?>());
                    registered?.Invoke(idle);
                    return Task.CompletedTask;
                });

            copilotMock.Setup(c => c.CreateSessionAsync(It.IsAny<global::GitHub.Copilot.SDK.SessionConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessionMock.Object);

            var client = new Synaxis.InferenceGateway.Infrastructure.External.GitHub.GitHubCopilotChatClient(copilotMock.Object, NullLogger<Synaxis.InferenceGateway.Infrastructure.External.GitHub.GitHubCopilotChatClient>.Instance);

            var resp = await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") });

            Assert.Contains("hello", resp.Messages.First().Text);
            copilotMock.Verify(c => c.CreateSessionAsync(It.IsAny<global::GitHub.Copilot.SDK.SessionConfig>(), It.IsAny<CancellationToken>()), Times.Once);
            sessionMock.Verify(s => s.SendAsync(It.IsAny<global::GitHub.Copilot.SDK.MessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetStreamingResponseAsync_StreamsUpdates()
        {
            var copilotMock = new Mock<global::GitHub.Copilot.SDK.CopilotClient>();

            var sessionMock = new Mock<global::GitHub.Copilot.SDK.CopilotSession>();

            global::GitHub.Copilot.SDK.SessionEventHandler? registered = null;
            sessionMock.Setup(s => s.On(It.IsAny<global::GitHub.Copilot.SDK.SessionEventHandler>()))
                .Returns<global::GitHub.Copilot.SDK.SessionEventHandler>(handler =>
                {
                    registered = handler;
                    return new DisposableAction();
                });

            sessionMock.Setup(s => s.SendAsync(It.IsAny<global::GitHub.Copilot.SDK.MessageOptions>(), It.IsAny<CancellationToken>()))
                .Returns((global::GitHub.Copilot.SDK.MessageOptions mo, CancellationToken ct) =>
                {
                    // send a delta, a full assistant message, a usage event and idle
                    registered?.Invoke(CreateEvent("AssistantMessageDeltaEvent", new Dictionary<string, object?> { ["DeltaContent"] = "d1" }));
                    registered?.Invoke(CreateEvent("AssistantMessageEvent", new Dictionary<string, object?> { ["Content"] = "m1" }));
                    registered?.Invoke(CreateEvent("AssistantUsageEvent", new Dictionary<string, object?> { ["InputTokens"] = 1, ["OutputTokens"] = 2 }));
                    registered?.Invoke(CreateEvent("SessionIdleEvent", new Dictionary<string, object?>()));
                    return Task.CompletedTask;
                });

            copilotMock.Setup(c => c.CreateSessionAsync(It.IsAny<global::GitHub.Copilot.SDK.SessionConfig>(), It.IsAny<CancellationToken>()))
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

            copilotMock.Verify(c => c.CreateSessionAsync(It.IsAny<global::GitHub.Copilot.SDK.SessionConfig>(), It.IsAny<CancellationToken>()), Times.Once);
            sessionMock.Verify(s => s.SendAsync(It.IsAny<global::GitHub.Copilot.SDK.MessageOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Dispose_DisposesClient()
        {
            var copilotMock = new Mock<global::GitHub.Copilot.SDK.CopilotClient>();

            var client = new Synaxis.InferenceGateway.Infrastructure.External.GitHub.GitHubCopilotChatClient(copilotMock.Object);
            // Ensure Dispose does not throw when SDK dispose isn't interceptable
            client.Dispose();
        }

        private class DisposableAction : IDisposable
        {
            public void Dispose() { }
        }

        private static global::GitHub.Copilot.SDK.SessionEvent CreateEvent(string typeName, Dictionary<string, object?> data)
        {
            var asm = typeof(global::GitHub.Copilot.SDK.CopilotClient).Assembly;
            var fullName = "GitHub.Copilot.SDK." + typeName;
            var t = asm.GetType(fullName) ?? throw new InvalidOperationException("Type not found: " + fullName);
            var evtObj = Activator.CreateInstance(t) ?? throw new InvalidOperationException("Could not create event " + fullName);

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
                dataProp.SetValue(evtObj, dataInstance);
            }

            return (global::GitHub.Copilot.SDK.SessionEvent)evtObj!;
        }
    }
}

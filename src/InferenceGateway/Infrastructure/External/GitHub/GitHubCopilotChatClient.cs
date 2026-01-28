using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub;

public class GitHubCopilotChatClient : IChatClient, IDisposable
{
    private readonly CopilotClient _copilotClient;
    private readonly ILogger<GitHubCopilotChatClient>? _logger;
    private readonly ChatClientMetadata _metadata = new ChatClientMetadata("GitHubCopilot", new Uri("https://copilot.github.com/"), "copilot");
    private readonly string _modelId = "copilot";

    public GitHubCopilotChatClient(CopilotClient copilotClient, ILogger<GitHubCopilotChatClient>? logger = null)
    {
        _copilotClient = copilotClient ?? throw new ArgumentNullException(nameof(copilotClient));
        _logger = logger;
    }

    public ChatClientMetadata Metadata => _metadata;

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Aggregate streaming updates into a single response for the non-streaming API
        var parts = new List<string>();
        await foreach (var update in GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            // collect text parts from updates
            foreach (var c in update.Contents)
            {
                if (c is TextContent tc)
                {
                    parts.Add(tc.Text ?? string.Empty);
                }
            }
        }

        var finalText = string.Join(string.Empty, parts.Where(p => !string.IsNullOrEmpty(p)));
        var resp = new ChatResponse(new ChatMessage(ChatRole.Assistant, finalText ?? string.Empty));
        resp.ModelId = _modelId;
        return resp;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (chatMessages is null) throw new ArgumentNullException(nameof(chatMessages));

        // Ensure client started
        if (_copilotClient.State != ConnectionState.Connected)
        {
            try
            {
                await _copilotClient.StartAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to start CopilotClient");
            }
        }

        // Always create a fresh streaming session for each call
        var sessionConfig = new SessionConfig { Streaming = true };
        CopilotSession copilotSession = await _copilotClient.CreateSessionAsync(sessionConfig, cancellationToken).ConfigureAwait(false);

        var channel = Channel.CreateUnbounded<ChatResponseUpdate>();

        IDisposable subscription = copilotSession.On(evt =>
        {
            try
            {
                switch (evt)
                {
                    case AssistantMessageDeltaEvent deltaEvent:
                        channel.Writer.TryWrite(ToUpdate(deltaEvent.Data?.DeltaContent ?? string.Empty, deltaEvent));
                        break;
                    case AssistantMessageEvent assistantMessage:
                        channel.Writer.TryWrite(ToUpdate(assistantMessage.Data?.Content ?? string.Empty, assistantMessage));
                        break;
                    case AssistantUsageEvent usageEvent:
                        // Map usage to a textual representation
                        var usageText = $"usage: input={usageEvent.Data?.InputTokens ?? 0} output={usageEvent.Data?.OutputTokens ?? 0}";
                        channel.Writer.TryWrite(ToUpdate(usageText, usageEvent));
                        break;
                    case SessionIdleEvent idleEvent:
                        channel.Writer.TryWrite(ToUpdate(string.Empty, idleEvent));
                        channel.Writer.TryComplete();
                        break;
                    case SessionErrorEvent errorEvent:
                        channel.Writer.TryWrite(ToUpdate(errorEvent.Data?.Message ?? "Session error", errorEvent));
                        channel.Writer.TryComplete(new InvalidOperationException(errorEvent.Data?.Message ?? "Session error"));
                        break;
                    default:
                        channel.Writer.TryWrite(ToUpdate(evt?.ToString() ?? string.Empty, evt));
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Error in Copilot event handler");
            }
        });

        try
        {
            // Build a simple text-only prompt
            string prompt = string.Join("\n", chatMessages.Select(m => m.Text));
            var messageOptions = new MessageOptions { Prompt = prompt };

            await copilotSession.SendAsync(messageOptions, cancellationToken).ConfigureAwait(false);

            await foreach (var update in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }
        }
        finally
        {
            subscription.Dispose();
            try { await copilotSession.DisposeAsync().ConfigureAwait(false); } catch { }
        }
    }

    private static ChatResponseUpdate ToUpdate(string text, object? raw)
    {
        var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
        var tc = new TextContent(text ?? string.Empty) { RawRepresentation = raw };
        update.Contents.Add(tc);
        return update;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(CopilotClient) || serviceType == typeof(CopilotClient?)) return _copilotClient;
        return null;
    }

    public void Dispose()
    {
        try { _copilotClient.DisposeAsync().GetAwaiter().GetResult(); } catch { }
    }
}

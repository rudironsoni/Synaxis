// <copyright file="GitHubCopilotChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using global::GitHub.Copilot.SDK;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// GitHubCopilotChatClient class.
    /// </summary>
    public sealed class GitHubCopilotChatClient : IChatClient
    {
        private readonly ICopilotClient _copilotClient;
        private readonly ILogger<GitHubCopilotChatClient>? _logger;
        private readonly ChatClientMetadata _metadata = new ChatClientMetadata("GitHubCopilot", new Uri("https://copilot.github.com/"), "copilot");
        private readonly string _modelId = "copilot";

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubCopilotChatClient"/> class.
        /// </summary>
        /// <param name="copilotClient">The Copilot client instance.</param>
        /// <param name="logger">Optional logger instance.</param>
        public GitHubCopilotChatClient(ICopilotClient copilotClient, ILogger<GitHubCopilotChatClient>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(copilotClient);
            this._copilotClient = copilotClient;
            this._logger = logger;
        }

        /// <summary>
        /// Gets the metadata for this chat client.
        /// </summary>
        public ChatClientMetadata Metadata => this._metadata;

        /// <inheritdoc/>
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            // Aggregate streaming updates into a single response for the non-streaming API
            var parts = new List<string>();
            await foreach (var update in this.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
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
            resp.ModelId = this._modelId;
            return resp;
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(messages);
            return this.GetStreamingResponseInternalAsync(messages, cancellationToken);
        }

        private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseInternalAsync(IEnumerable<ChatMessage> messages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Ensure client started
            await this.EnsureClientStartedAsync(cancellationToken).ConfigureAwait(false);

            // Always create a fresh streaming session for each call
            var sessionConfig = new SessionConfig { Streaming = true };
            ICopilotSession copilotSession = await this._copilotClient.CreateSessionAsync(sessionConfig, cancellationToken).ConfigureAwait(false);

            var channel = Channel.CreateUnbounded<ChatResponseUpdate>();
            IDisposable subscription = this.SubscribeToSessionEvents(copilotSession, channel);

            try
            {
                await this.SendPromptAndStreamResponseAsync(copilotSession, messages, cancellationToken).ConfigureAwait(false);

                await foreach (var update in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return update;
                }
            }
            finally
            {
                subscription.Dispose();
                try
                {
                    await copilotSession.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Suppress disposal exceptions to prevent masking primary exceptions
                    this._logger?.LogDebug(ex, "Error disposing Copilot session");
                }
            }
        }

        private async Task EnsureClientStartedAsync(CancellationToken cancellationToken)
        {
            if (this._copilotClient.State != ConnectionState.Connected)
            {
                try
                {
                    await this._copilotClient.StartAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger?.LogWarning(ex, "Failed to start CopilotClient");
                }
            }
        }

        private IDisposable SubscribeToSessionEvents(ICopilotSession copilotSession, Channel<ChatResponseUpdate> channel)
        {
            return copilotSession.On(evt =>
            {
                try
                {
                    HandleSessionEvent(evt, channel);
                }
                catch (Exception ex)
                {
                    this._logger?.LogDebug(ex, "Error in Copilot event handler");
                }
            });
        }

        private static void HandleSessionEvent(object? evt, Channel<ChatResponseUpdate> channel)
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

        private Task SendPromptAndStreamResponseAsync(
            ICopilotSession copilotSession,
            IEnumerable<ChatMessage> messages,
            CancellationToken cancellationToken)
        {
            string prompt = string.Join("\n", messages.Select(m => m.Text));
            var messageOptions = new MessageOptions { Prompt = prompt };
            return copilotSession.SendAsync(messageOptions, cancellationToken);
        }

        private static ChatResponseUpdate ToUpdate(string text, object? raw)
        {
            var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
            var tc = new TextContent(text ?? string.Empty) { RawRepresentation = raw };
            update.Contents.Add(tc);
            return update;
        }

        /// <inheritdoc/>
        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (serviceType == typeof(ICopilotClient))
            {
                return this._copilotClient;
            }

            return null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Note: _copilotClient is injected and should not be disposed by this class.
            // The caller/DI container is responsible for disposing the injected ICopilotClient.
        }
    }
}

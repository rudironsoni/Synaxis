// <copyright file="ChatHub.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.SignalR.Hubs
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Mediator;
    using Microsoft.AspNetCore.SignalR;
    using Synaxis.Adapters.SignalR.Connection;
    using Synaxis.Commands.Chat;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// SignalR hub for chat completion operations.
    /// </summary>
    public sealed class ChatHub : Hub
    {
        private readonly IMediator mediator;
        private readonly ConnectionManager connectionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatHub"/> class.
        /// </summary>
        /// <param name="mediator">The mediator for executing commands.</param>
        /// <param name="connectionManager">The connection manager for tracking connections.</param>
        public ChatHub(IMediator mediator, ConnectionManager connectionManager)
        {
            this.mediator = mediator!;
            this.connectionManager = connectionManager!;
        }

        /// <summary>
        /// Streams chat completion chunks in real-time.
        /// </summary>
        /// <param name="command">The streaming chat command.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>An async enumerable of chat stream chunks.</returns>
        public IAsyncEnumerable<ChatStreamChunk> StreamChat(ChatStreamCommand command, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(command);
            return this.StreamChatCore(command, ct);
        }

        private async IAsyncEnumerable<ChatStreamChunk> StreamChatCore(
            ChatStreamCommand command,
            [EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var chunk in this.mediator.CreateStream(command, ct).ConfigureAwait(false))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Sends a chat completion request and returns the full response.
        /// </summary>
        /// <param name="command">The chat command.</param>
        /// <returns>A task that represents the asynchronous operation, containing the chat response.</returns>
        public async Task<ChatResponse> SendChat(ChatCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            return await this.mediator.Send(command).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override Task OnConnectedAsync()
        {
            this.connectionManager.Add(this.Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        /// <inheritdoc/>
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            this.connectionManager.Remove(this.Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}

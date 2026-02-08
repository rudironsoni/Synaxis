// <copyright file="SynaxisHub.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.SignalR.Hubs
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR;
    using Synaxis.Adapters.SignalR.Connection;

    /// <summary>
    /// General-purpose SignalR hub for Synaxis operations.
    /// </summary>
    public sealed class SynaxisHub : Hub
    {
        private readonly ConnectionManager connectionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynaxisHub"/> class.
        /// </summary>
        /// <param name="connectionManager">The connection manager for tracking connections.</param>
        public SynaxisHub(ConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <summary>
        /// Joins a SignalR group.
        /// </summary>
        /// <param name="groupId">The group identifier to join.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task JoinGroup(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                throw new ArgumentException("Group ID cannot be null or whitespace.", nameof(groupId));
            }

            return this.Groups.AddToGroupAsync(this.Context.ConnectionId, groupId);
        }

        /// <summary>
        /// Leaves a SignalR group.
        /// </summary>
        /// <param name="groupId">The group identifier to leave.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task LeaveGroup(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                throw new ArgumentException("Group ID cannot be null or whitespace.", nameof(groupId));
            }

            return this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, groupId);
        }

        /// <summary>
        /// Gets the current connection identifier.
        /// </summary>
        /// <returns>The connection identifier.</returns>
        public string GetConnectionId()
        {
            return this.Context.ConnectionId;
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

// <copyright file="GroupManager.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.SignalR.Groups
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR;

    /// <summary>
    /// Manages SignalR group memberships.
    /// </summary>
    public sealed class GroupManager
    {
        private readonly IHubContext<Hubs.SynaxisHub> hubContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupManager"/> class.
        /// </summary>
        /// <param name="hubContext">The hub context for group operations.</param>
        public GroupManager(IHubContext<Hubs.SynaxisHub> hubContext)
        {
            this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        /// <summary>
        /// Adds a connection to a group.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task AddToGroupAsync(string connectionId, string groupId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                throw new ArgumentException("Connection ID cannot be null or whitespace.", nameof(connectionId));
            }

            if (string.IsNullOrWhiteSpace(groupId))
            {
                throw new ArgumentException("Group ID cannot be null or whitespace.", nameof(groupId));
            }

            return this.hubContext.Groups.AddToGroupAsync(connectionId, groupId);
        }

        /// <summary>
        /// Removes a connection from a group.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task RemoveFromGroupAsync(string connectionId, string groupId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                throw new ArgumentException("Connection ID cannot be null or whitespace.", nameof(connectionId));
            }

            if (string.IsNullOrWhiteSpace(groupId))
            {
                throw new ArgumentException("Group ID cannot be null or whitespace.", nameof(groupId));
            }

            return this.hubContext.Groups.RemoveFromGroupAsync(connectionId, groupId);
        }

        /// <summary>
        /// Sends a message to all connections in a group.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="method">The method name to invoke on clients.</param>
        /// <param name="args">The arguments to pass to the client method.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task SendToGroupAsync(string groupId, string method, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                throw new ArgumentException("Group ID cannot be null or whitespace.", nameof(groupId));
            }

            if (string.IsNullOrWhiteSpace(method))
            {
                throw new ArgumentException("Method name cannot be null or whitespace.", nameof(method));
            }

            return this.hubContext.Clients.Group(groupId).SendCoreAsync(method, args);
        }
    }
}

// <copyright file="ConnectionManager.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.SignalR.Connection
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Manages SignalR connection metadata and lifecycle.
    /// </summary>
    public sealed class ConnectionManager
    {
        private readonly ConcurrentDictionary<string, ConnectionMetadata> connections = new (StringComparer.Ordinal);

        /// <summary>
        /// Adds a new connection with metadata.
        /// </summary>
        /// <param name="connectionId">The unique connection identifier.</param>
        /// <param name="metadata">Optional metadata for the connection.</param>
        public void Add(string connectionId, IDictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                throw new ArgumentException("Connection ID cannot be null or whitespace.", nameof(connectionId));
            }

            var connectionMetadata = new ConnectionMetadata
            {
                ConnectionId = connectionId,
                ConnectedAt = DateTimeOffset.UtcNow,
                Metadata = metadata ?? new Dictionary<string, string>(StringComparer.Ordinal),
            };

            this.connections.TryAdd(connectionId, connectionMetadata);
        }

        /// <summary>
        /// Removes a connection.
        /// </summary>
        /// <param name="connectionId">The unique connection identifier.</param>
        /// <returns>True if the connection was removed; otherwise, false.</returns>
        public bool Remove(string connectionId)
        {
            return this.connections.TryRemove(connectionId, out _);
        }

        /// <summary>
        /// Gets metadata for a specific connection.
        /// </summary>
        /// <param name="connectionId">The unique connection identifier.</param>
        /// <returns>The connection metadata, or null if not found.</returns>
        public ConnectionMetadata? Get(string connectionId)
        {
            this.connections.TryGetValue(connectionId, out var metadata);
            return metadata;
        }

        /// <summary>
        /// Gets all active connections.
        /// </summary>
        /// <returns>A collection of all connection metadata.</returns>
        public IReadOnlyCollection<ConnectionMetadata> GetAll()
        {
            return this.connections.Values.ToList();
        }

        /// <summary>
        /// Gets the count of active connections.
        /// </summary>
        /// <returns>The number of active connections.</returns>
        public int Count()
        {
            return this.connections.Count;
        }
    }
}

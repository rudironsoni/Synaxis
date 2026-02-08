// <copyright file="ConnectionMetadata.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.SignalR.Connection
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents metadata for a SignalR connection.
    /// </summary>
    public sealed class ConnectionMetadata
    {
        /// <summary>
        /// Gets or sets the unique connection identifier.
        /// </summary>
        public string ConnectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the connection was established.
        /// </summary>
        public DateTimeOffset ConnectedAt { get; set; }

        /// <summary>
        /// Gets or sets custom metadata associated with the connection.
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}

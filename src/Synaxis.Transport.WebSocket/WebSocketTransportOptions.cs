// <copyright file="WebSocketTransportOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.WebSocket
{
    using System;

    /// <summary>
    /// Configuration options for WebSocket transport layer.
    /// </summary>
    public class WebSocketTransportOptions
    {
        /// <summary>
        /// Gets or sets the WebSocket endpoint path.
        /// </summary>
        public string Path { get; set; } = "/ws";

        /// <summary>
        /// Gets or sets the receive buffer size in bytes.
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 1024 * 4;

        /// <summary>
        /// Gets or sets the keep-alive interval.
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30);
    }
}

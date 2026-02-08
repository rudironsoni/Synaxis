// <copyright file="WebSocketMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.WebSocket.Protocol
{
    using System.Text.Json;

    /// <summary>
    /// Represents a message transmitted over WebSocket connection.
    /// </summary>
    public class WebSocketMessage
    {
        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public string Type { get; set; } = "command";

        /// <summary>
        /// Gets or sets the command type name.
        /// </summary>
        public string CommandType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message payload.
        /// </summary>
        public JsonElement Payload { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier for tracking request-response pairs.
        /// </summary>
        public string? CorrelationId { get; set; }
    }
}

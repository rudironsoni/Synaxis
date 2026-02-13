// <copyright file="MessageSerializer.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.WebSocket.Protocol
{
    using System;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides serialization and deserialization for WebSocket messages.
    /// </summary>
    public static class MessageSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        /// <summary>
        /// Serializes a WebSocket message to JSON bytes.
        /// </summary>
        /// <param name="message">The message to serialize.</param>
        /// <returns>A byte array containing the serialized message.</returns>
        /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
        public static byte[] Serialize(WebSocketMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            string json = JsonSerializer.Serialize(message, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Deserializes a WebSocket message from JSON bytes.
        /// </summary>
        /// <param name="data">The byte array containing the serialized message.</param>
        /// <returns>The deserialized WebSocket message.</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
        /// <exception cref="JsonException">Thrown when deserialization fails.</exception>
        public static WebSocketMessage Deserialize(byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<WebSocketMessage>(json, JsonOptions)
                ?? throw new JsonException("Failed to deserialize WebSocket message");
        }

        /// <summary>
        /// Deserializes a WebSocket message from JSON bytes asynchronously.
        /// </summary>
        /// <param name="data">The byte array containing the serialized message.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the deserialized message.</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
        /// <exception cref="JsonException">Thrown when deserialization fails.</exception>
        public static async ValueTask<WebSocketMessage> DeserializeAsync(
            byte[] data,
            CancellationToken cancellationToken = default)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            using var stream = new System.IO.MemoryStream(data);
            return await JsonSerializer.DeserializeAsync<WebSocketMessage>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new JsonException("Failed to deserialize WebSocket message");
        }
    }
}

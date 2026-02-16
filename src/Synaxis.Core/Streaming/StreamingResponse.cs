// <copyright file="StreamingResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Streaming
{
    using System;

    /// <summary>
    /// Represents a streaming response for Server-Sent Events (SSE).
    /// </summary>
    public class StreamingResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the streaming response.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the event type for the SSE message.
        /// </summary>
        public string Event { get; set; } = "message";

        /// <summary>
        /// Gets or sets the data payload for the streaming response.
        /// </summary>
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the retry interval in milliseconds.
        /// </summary>
        public int? Retry { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the final chunk.
        /// </summary>
        public bool IsDone { get; set; }

        /// <summary>
        /// Gets or sets the error message if an error occurred.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the response.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the metadata associated with the streaming response.
        /// </summary>
        public StreamingMetadata? Metadata { get; set; }

        /// <summary>
        /// Creates a new streaming response with the specified data.
        /// </summary>
        /// <param name="data">The data payload.</param>
        /// <param name="eventType">The event type.</param>
        /// <returns>A new streaming response.</returns>
        public static StreamingResponse Create(string data, string eventType = "message")
        {
            return new StreamingResponse
            {
                Data = data,
                Event = eventType,
            };
        }

        /// <summary>
        /// Creates a done response to signal the end of the stream.
        /// </summary>
        /// <returns>A done streaming response.</returns>
        public static StreamingResponse CreateDone()
        {
            return new StreamingResponse
            {
                Data = "[DONE]",
                Event = "done",
                IsDone = true,
            };
        }

        /// <summary>
        /// Creates an error response.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>An error streaming response.</returns>
        public static StreamingResponse CreateError(string errorMessage)
        {
            return new StreamingResponse
            {
                Data = string.Empty,
                Event = "error",
                Error = errorMessage,
                IsDone = true,
            };
        }

        /// <summary>
        /// Converts the streaming response to SSE format.
        /// </summary>
        /// <returns>The SSE formatted string.</returns>
        public string ToSseFormat()
        {
            var sse = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(this.Id))
            {
                sse.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"id: {this.Id}");
            }

            if (!string.IsNullOrEmpty(this.Event))
            {
                sse.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"event: {this.Event}");
            }

            if (this.Retry.HasValue)
            {
                sse.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"retry: {this.Retry.Value}");
            }

            if (!string.IsNullOrEmpty(this.Data))
            {
                // Handle multi-line data by prefixing each line with "data: "
                var lines = this.Data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    sse.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"data: {line}");
                }
            }

            sse.AppendLine(); // Empty line to end the message

            return sse.ToString();
        }
    }
}

// <copyright file="StreamingMetadata.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Streaming
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents metadata associated with a streaming response.
    /// </summary>
    public class StreamingMetadata
    {
        /// <summary>
        /// Gets or sets the provider name (e.g., "openai", "anthropic").
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token usage information.
        /// </summary>
        public TokenUsage Usage { get; set; }

        /// <summary>
        /// Gets or sets the finish reason (e.g., "stop", "length", "content_filter").
        /// </summary>
        public string FinishReason { get; set; }

        /// <summary>
        /// Gets or sets additional custom metadata.
        /// </summary>
        public IDictionary<string, object> Custom { get; set; }
    }
}

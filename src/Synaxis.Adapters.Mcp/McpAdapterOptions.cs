// <copyright file="McpAdapterOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp
{
    /// <summary>
    /// Configuration options for the MCP adapter.
    /// </summary>
    public sealed class McpAdapterOptions
    {
        /// <summary>
        /// Gets or sets the MCP server name.
        /// </summary>
        public string ServerName { get; set; } = "synaxis";

        /// <summary>
        /// Gets or sets the MCP server version.
        /// </summary>
        public string ServerVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the default transport type.
        /// </summary>
        public McpTransportType DefaultTransport { get; set; } = McpTransportType.Stdio;

        /// <summary>
        /// Gets or sets the HTTP transport base URL (only used when DefaultTransport is Http).
        /// </summary>
        public string HttpBaseUrl { get; set; } = "http://localhost:5000";

        /// <summary>
        /// Gets or sets the SSE transport endpoint (only used when DefaultTransport is Sse).
        /// </summary>
        public string SseEndpoint { get; set; } = "http://localhost:5000/sse";
    }
}

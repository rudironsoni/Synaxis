// <copyright file="McpTransportType.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp
{
    /// <summary>
    /// Defines the available MCP transport types.
    /// </summary>
    public enum McpTransportType
    {
        /// <summary>
        /// Standard input/output transport for CLI tools.
        /// </summary>
        Stdio,

        /// <summary>
        /// HTTP REST API transport for web clients.
        /// </summary>
        Http,

        /// <summary>
        /// Server-Sent Events transport for real-time updates.
        /// </summary>
        Sse,
    }
}

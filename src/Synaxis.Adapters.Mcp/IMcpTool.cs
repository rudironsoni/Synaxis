// <copyright file="IMcpTool.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp
{
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for an MCP tool that can be invoked remotely.
    /// </summary>
    public interface IMcpTool
    {
        /// <summary>
        /// Gets the unique name of the tool.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the human-readable description of what the tool does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the JSON schema that defines the tool's input parameters.
        /// </summary>
        JsonDocument InputSchema { get; }

        /// <summary>
        /// Executes the tool with the provided arguments.
        /// </summary>
        /// <param name="arguments">The input arguments as a JSON element.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the tool result.</returns>
        Task<object> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken);
    }
}

// <copyright file="SynaxisMcpServer.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.Server
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Execution;
    using Synaxis.Adapters.Mcp.Tools;
    using Synaxis.Commands.Chat;
    using Synaxis.Commands.Embeddings;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// MCP server implementation that exposes Synaxis capabilities as tools.
    /// </summary>
    public sealed class SynaxisMcpServer
    {
        private readonly IToolRegistry _registry;
        private readonly ILogger<SynaxisMcpServer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynaxisMcpServer"/> class.
        /// </summary>
        /// <param name="chatExecutor">The chat command executor.</param>
        /// <param name="embeddingExecutor">The embedding command executor.</param>
        /// <param name="logger">The logger instance.</param>
        public SynaxisMcpServer(
            ICommandExecutor<ChatCommand, ChatResponse> chatExecutor,
            ICommandExecutor<EmbeddingCommand, EmbeddingResponse> embeddingExecutor,
            ILogger<SynaxisMcpServer> logger)
        {
            ArgumentNullException.ThrowIfNull(chatExecutor);
            ArgumentNullException.ThrowIfNull(embeddingExecutor);
            this._logger = logger!;

            this._registry = new ToolRegistry();
            this.ConfigureTools(chatExecutor, embeddingExecutor);
        }

        /// <summary>
        /// Gets the tool registry containing all registered tools.
        /// </summary>
        public IToolRegistry Registry => this._registry;

        /// <summary>
        /// Executes a tool by name with the provided arguments.
        /// </summary>
        /// <param name="toolName">The name of the tool to execute.</param>
        /// <param name="arguments">The input arguments as a JSON element.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the tool result.</returns>
        public async Task<object> ExecuteToolAsync(
            string toolName,
            JsonElement arguments,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                throw new ArgumentException("Tool name cannot be null or whitespace.", nameof(toolName));
            }

            var tool = this._registry.GetByName(toolName);
            if (tool is null)
            {
                throw new InvalidOperationException($"Tool '{toolName}' not found.");
            }

            this._logger.LogInformation("Executing tool: {ToolName}", toolName);

            try
            {
                var result = await tool.ExecuteAsync(arguments, cancellationToken).ConfigureAwait(false);
                this._logger.LogInformation("Tool {ToolName} executed successfully", toolName);
                return result;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error executing tool {ToolName}", toolName);
                throw new InvalidOperationException($"Tool execution failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Configures and registers all available tools.
        /// </summary>
        private void ConfigureTools(
            ICommandExecutor<ChatCommand, ChatResponse> chatExecutor,
            ICommandExecutor<EmbeddingCommand, EmbeddingResponse> embeddingExecutor)
        {
            this._registry.Register(new ChatCompletionTool(chatExecutor));
            this._registry.Register(new EmbeddingTool(embeddingExecutor));

            this._logger.LogInformation("Registered {ToolCount} MCP tools", this._registry.GetAll().Count);
        }
    }
}

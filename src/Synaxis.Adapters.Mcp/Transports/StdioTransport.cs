// <copyright file="StdioTransport.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.Transports
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Adapters.Mcp.Server;

    /// <summary>
    /// Standard input/output transport for CLI tools using JSON-RPC protocol.
    /// </summary>
    public sealed class StdioTransport : IMcpTransport
    {
        private readonly SynaxisMcpServer _server;
        private readonly ILogger<StdioTransport> _logger;
        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="StdioTransport"/> class.
        /// </summary>
        /// <param name="server">The MCP server instance.</param>
        /// <param name="logger">The logger instance.</param>
        public StdioTransport(SynaxisMcpServer server, ILogger<StdioTransport> logger)
        {
            this._server = server ?? throw new ArgumentNullException(nameof(server));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("Starting STDIO transport");
            this._cancellationTokenSource?.Dispose();
            this._cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            return Task.Run(
                async () =>
            {
                await this.ProcessStdioAsync(this._cancellationTokenSource.Token).ConfigureAwait(false);
            },
                this._cancellationTokenSource.Token);
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("Stopping STDIO transport");
            this._cancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            this._cancellationTokenSource?.Dispose();
            return ValueTask.CompletedTask;
        }

        private async Task ProcessStdioAsync(CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(Console.OpenStandardInput());
            using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true, };

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                try
                {
                    var request = JsonSerializer.Deserialize<JsonRpcRequest>(line);
                    if (request is null)
                    {
                        continue;
                    }

                    var response = await this.HandleRequestAsync(request, cancellationToken).ConfigureAwait(false);
                    var responseJson = JsonSerializer.Serialize(response);
                    await writer.WriteLineAsync(responseJson).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error processing STDIO request");
                    var errorResponse = new JsonRpcResponse
                    {
                        Id = null,
                        Error = new JsonRpcError
                        {
                            Code = -32603,
                            Message = "Internal error",
                        },
                    };
                    var errorJson = JsonSerializer.Serialize(errorResponse);
                    await writer.WriteLineAsync(errorJson).ConfigureAwait(false);
                }
            }
        }

        private async Task<JsonRpcResponse> HandleRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (string.Equals(request.Method, "tools/call", StringComparison.Ordinal))
            {
                var toolName = request.Params?.GetProperty("name").GetString();
                var arguments = request.Params?.GetProperty("arguments") ?? default;

                if (string.IsNullOrWhiteSpace(toolName))
                {
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Error = new JsonRpcError
                        {
                            Code = -32602,
                            Message = "Invalid params: tool name is required",
                        },
                    };
                }

                var result = await this._server.ExecuteToolAsync(toolName, arguments, cancellationToken).ConfigureAwait(false);
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = result,
                };
            }
            else if (string.Equals(request.Method, "tools/list", StringComparison.Ordinal))
            {
                var tools = this._server.Registry.GetAll();
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new { tools, },
                };
            }

            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError
                {
                    Code = -32601,
                    Message = $"Method not found: {request.Method}",
                },
            };
        }

        private sealed class JsonRpcRequest
        {
            public string Jsonrpc { get; set; } = "2.0";

            public string? Id { get; set; }

            public string Method { get; set; } = string.Empty;

            public JsonElement? Params { get; set; }
        }

        private sealed class JsonRpcResponse
        {
            public string Jsonrpc { get; set; } = "2.0";

            public string? Id { get; set; }

            public object? Result { get; set; }

            public JsonRpcError? Error { get; set; }
        }

        private sealed class JsonRpcError
        {
            public int Code { get; set; }

            public string Message { get; set; } = string.Empty;
        }
    }
}

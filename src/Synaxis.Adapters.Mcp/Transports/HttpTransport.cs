// <copyright file="HttpTransport.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.Transports
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Adapters.Mcp.Server;

    /// <summary>
    /// HTTP transport for web clients using REST API.
    /// </summary>
    public sealed class HttpTransport : IMcpTransport
    {
        private readonly ILogger<HttpTransport> _logger;
        private readonly string _baseUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpTransport"/> class.
        /// </summary>
        /// <param name="server">The MCP server instance.</param>
        /// <param name="baseUrl">The base URL for the HTTP server.</param>
        /// <param name="logger">The logger instance.</param>
        public HttpTransport(
            SynaxisMcpServer server,
            string baseUrl,
            ILogger<HttpTransport> logger)
        {
            _ = server ?? throw new ArgumentNullException(nameof(server));
            this._baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("HTTP transport would start at {BaseUrl}", this._baseUrl);

            // Note: Actual HTTP server implementation would be integrated with ASP.NET Core
            // This is a placeholder that demonstrates the structure
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("HTTP transport stopped");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}

// <copyright file="IMcpTransport.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.Transports
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for MCP transport mechanisms.
    /// </summary>
    public interface IMcpTransport : IAsyncDisposable
    {
        /// <summary>
        /// Starts the transport and begins listening for requests.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the transport and releases resources.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StopAsync(CancellationToken cancellationToken);
    }
}

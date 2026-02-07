// <copyright file="ICopilotClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::GitHub.Copilot.SDK;

    /// <summary>
    /// Interface for a GitHub Copilot client.
    /// </summary>
    public interface ICopilotClient : IAsyncDisposable
    {
        /// <summary>
        /// Gets the current connection state of the Copilot client.
        /// </summary>
        ConnectionState State { get; }

        /// <summary>
        /// Starts the Copilot client connection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new Copilot session with the specified configuration.
        /// </summary>
        /// <param name="config">The session configuration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that resolves to a Copilot session.</returns>
        Task<ICopilotSession> CreateSessionAsync(SessionConfig config, CancellationToken cancellationToken = default);
    }
}

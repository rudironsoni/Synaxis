// <copyright file="ICopilotSession.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::GitHub.Copilot.SDK;

    /// <summary>
    /// Interface for a GitHub Copilot session.
    /// </summary>
    public interface ICopilotSession : IAsyncDisposable
    {
        /// <summary>
        /// Registers an event handler for session events.
        /// </summary>
        /// <param name="handler">The event handler to register.</param>
        /// <returns>A disposable subscription.</returns>
        IDisposable On(SessionEventHandler handler);

        /// <summary>
        /// Sends a message to the Copilot session.
        /// </summary>
        /// <param name="options">The message options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendAsync(MessageOptions options, CancellationToken cancellationToken = default);
    }
}

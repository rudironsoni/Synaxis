// <copyright file="CopilotSessionAdapter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::GitHub.Copilot.SDK;

    /// <summary>
    /// Adapter for GitHub Copilot session.
    /// </summary>
    internal sealed class CopilotSessionAdapter : ICopilotSession
    {
        private readonly CopilotSession _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="CopilotSessionAdapter"/> class.
        /// </summary>
        /// <param name="inner">The inner Copilot session.</param>
        public CopilotSessionAdapter(CopilotSession inner)
        {
            ArgumentNullException.ThrowIfNull(inner);
            this._inner = inner;
        }

        /// <inheritdoc/>
        public IDisposable On(SessionEventHandler handler) => this._inner.On(handler);

        /// <inheritdoc/>
        public Task SendAsync(MessageOptions options, CancellationToken cancellationToken = default) => this._inner.SendAsync(options, cancellationToken);

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            // Note: _inner is injected and should not be disposed by this adapter.
            // The caller/DI container is responsible for disposing the injected CopilotSession.
            return ValueTask.CompletedTask;
        }
    }
}
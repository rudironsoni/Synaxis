// <copyright file="CopilotClientAdapter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::GitHub.Copilot.SDK;

    internal class CopilotSessionAdapter : ICopilotSession
    {
        private readonly CopilotSession _inner;

        public CopilotSessionAdapter(CopilotSession inner) => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        public IDisposable On(SessionEventHandler handler) => _inner.On(handler);

        public Task SendAsync(MessageOptions options, CancellationToken cancellationToken = default) => _inner.SendAsync(options, cancellationToken);

        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }

    internal class CopilotClientAdapter : ICopilotClient
    {
        private readonly CopilotClient _inner;

        public CopilotClientAdapter(CopilotClient inner) => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        public ConnectionState State => _inner.State;

        public Task StartAsync(CancellationToken cancellationToken = default) => _inner.StartAsync(cancellationToken);

        public async Task<ICopilotSession> CreateSessionAsync(SessionConfig config, CancellationToken cancellationToken = default)
        {
            var s = await _inner.CreateSessionAsync(config, cancellationToken).ConfigureAwait(false);
            return new CopilotSessionAdapter(s);
        }

        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}

// <copyright file="ICopilotClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::GitHub.Copilot.SDK;

    public interface ICopilotClient : IAsyncDisposable
    {
        ConnectionState State { get; }
        Task StartAsync(CancellationToken cancellationToken = default);
        Task<ICopilotSession> CreateSessionAsync(SessionConfig config, CancellationToken cancellationToken = default);
    }
}

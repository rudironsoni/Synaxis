// <copyright file="ICopilotSession.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::GitHub.Copilot.SDK;

    public interface ICopilotSession : IAsyncDisposable
    {
        IDisposable On(SessionEventHandler handler);
        Task SendAsync(MessageOptions options, CancellationToken cancellationToken = default);
    }
}

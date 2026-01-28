using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Copilot.SDK;

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    public interface ICopilotClient : IAsyncDisposable
    {
        ConnectionState State { get; }
        Task StartAsync(CancellationToken cancellationToken = default);
        Task<ICopilotSession> CreateSessionAsync(SessionConfig config, CancellationToken cancellationToken = default);
    }
}

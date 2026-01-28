using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Copilot.SDK;

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    public interface ICopilotSession : IAsyncDisposable
    {
        IDisposable On(SessionEventHandler handler);
        Task SendAsync(MessageOptions options, CancellationToken cancellationToken = default);
    }
}

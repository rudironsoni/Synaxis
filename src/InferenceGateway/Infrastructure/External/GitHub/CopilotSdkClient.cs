using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub;

/// <summary>
/// Adapter interface used by CopilotSdkClient to allow easy testing and to separate the
/// concrete GitHub.Copilot.SDK usage. A real implementation should wrap the SDK and
/// translate messages to/from its types.
/// </summary>
public interface ICopilotSdkAdapter : IDisposable
{
    ChatClientMetadata Metadata { get; }
    Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default);
    object? GetService(Type serviceType, object? serviceKey = null);
}

/// <summary>
/// IChatClient implementation backed by a GitHub Copilot local SDK adapter.
/// The concrete SDK wiring should be provided via an ICopilotSdkAdapter instance.
/// This keeps the production wiring (which may require a running local agent process)
/// out of the tests and allows graceful lifecycle management.
/// </summary>
public class CopilotSdkClient : IChatClient
{
    private readonly ICopilotSdkAdapter _adapter;

    public CopilotSdkClient(ICopilotSdkAdapter adapter)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    // Note: For scenarios where the SDK is available at runtime a factory/wrapper can be
    // written to produce an ICopilotSdkAdapter that talks to GitHub.Copilot.SDK. That factory
    // is intentionally not included here to keep the client lightweight and testable.

    public ChatClientMetadata Metadata => _adapter.Metadata ?? new ChatClientMetadata("Copilot");

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _adapter.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _adapter.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return _adapter.GetService(serviceType, serviceKey);
    }

    public void Dispose()
    {
        _adapter.Dispose();
    }
}

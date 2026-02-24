using Microsoft.Extensions.AI;

namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

/// <summary>
/// Interface for in-flight request deduplication
/// </summary>
public interface IInFlightDeduplicationService
{
    Task<ChatResponse?> TryGetInFlightAsync(string fingerprint, CancellationToken cancellationToken);

    Task RegisterInFlightAsync(string fingerprint, Task<ChatResponse> responseTask, CancellationToken cancellationToken);
}

using Microsoft.Extensions.AI;

namespace Synaxis.Common.Tests;

/// <summary>
/// Interface for in-flight request deduplication.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface IInFlightDeduplicationService
{
    Task<ChatResponse?> TryGetInFlightAsync(string fingerprint, CancellationToken cancellationToken);

    Task RegisterInFlightAsync(string fingerprint, Task<ChatResponse> responseTask, CancellationToken cancellationToken);
}

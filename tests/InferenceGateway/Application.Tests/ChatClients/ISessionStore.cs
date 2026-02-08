namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

/// <summary>
/// Interface for managing session affinity
/// </summary>
public interface ISessionStore
{
    Task<string?> GetPreferredProviderAsync(string sessionId, CancellationToken cancellationToken);

    Task SetPreferredProviderAsync(string sessionId, string providerId, CancellationToken cancellationToken);
}

namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;

/// <summary>
/// Interface for semantic cache service
/// </summary>
public interface ISemanticCacheService
{
    Task<CacheResult> TryGetCachedAsync(
        string query,
        string sessionId,
        string model,
        double temperature,
        CancellationToken cancellationToken);

    Task StoreAsync(
        string query,
        string response,
        string sessionId,
        string model,
        double temperature,
        float[]? embedding,
        CancellationToken cancellationToken);

    Task<int> InvalidateSessionAsync(
        string sessionId,
        CancellationToken cancellationToken);
}

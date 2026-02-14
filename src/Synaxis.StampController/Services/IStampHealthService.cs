// =============================================================================
// Stamp Health Service Interface
// =============================================================================

using Synaxis.StampController.CRDs;

namespace Synaxis.StampController.Services;

/// <summary>
/// Service for checking stamp health
/// </summary>
public interface IStampHealthService
{
    /// <summary>
    /// Performs comprehensive health check on a stamp
    /// </summary>
    Task<StampHealth> CheckHealthAsync(StampResource stamp, CancellationToken cancellationToken);

    /// <summary>
    /// Checks Kubernetes cluster health
    /// </summary>
    Task<HealthStatus> CheckKubernetesHealthAsync(StampResource stamp, CancellationToken cancellationToken);

    /// <summary>
    /// Checks networking health
    /// </summary>
    Task<HealthStatus> CheckNetworkingHealthAsync(StampResource stamp, CancellationToken cancellationToken);

    /// <summary>
    /// Checks storage health
    /// </summary>
    Task<HealthStatus> CheckStorageHealthAsync(StampResource stamp, CancellationToken cancellationToken);
}

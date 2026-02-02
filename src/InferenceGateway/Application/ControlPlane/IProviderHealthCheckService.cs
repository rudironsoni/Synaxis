namespace Synaxis.InferenceGateway.Application.ControlPlane;

using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

/// <summary>
/// Service for checking provider health.
/// </summary>
public interface IProviderHealthCheckService
{
    /// <summary>
    /// Checks the health of a specific provider.
    /// </summary>
    /// <param name="providerKey">The provider key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    Task<HealthCheckResult> CheckProviderHealthAsync(string providerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a health check and updates the health store.
    /// </summary>
    /// <param name="providerKey">The provider key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    Task<HealthCheckResult> RunHealthCheckAsync(string providerKey, CancellationToken cancellationToken = default);
}

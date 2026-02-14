using System.Threading.Tasks;

namespace Synaxis.Routing.Health;

/// <summary>
/// Interface for checking the health of AI providers.
/// </summary>
public interface IProviderHealthChecker
{
    /// <summary>
    /// Performs a health check on a provider.
    /// </summary>
    /// <param name="providerId">The provider ID to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The health check result.</returns>
    Task<ProviderHealthCheckResult> CheckHealthAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs health checks on multiple providers concurrently.
    /// </summary>
    /// <param name="providerIds">The provider IDs to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A dictionary of provider IDs to their health check results.</returns>
    Task<Dictionary<string, ProviderHealthCheckResult>> CheckHealthAsync(
        IEnumerable<string> providerIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current health status of a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The current health status.</returns>
    Task<ProviderHealthStatus> GetHealthStatusAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health history for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A list of health check results.</returns>
    Task<List<ProviderHealthCheckResult>> GetHealthHistoryAsync(
        string providerId,
        int limit = 100,
        CancellationToken cancellationToken = default);
}

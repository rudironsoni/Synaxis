namespace Synaxis.InferenceGateway.Application.ControlPlane;

/// <summary>
/// Service for monitoring quota usage and sending warnings.
/// </summary>
public interface IQuotaWarningService
{
    /// <summary>
    /// Checks if quota is below warning threshold and sends warning if needed.
    /// </summary>
    /// <param name="providerKey">The provider key</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if warning was sent, false otherwise</returns>
    Task<bool> CheckQuotaWarningAsync(
        string providerKey,
        string? tenantId = null,
        string? userId = null,
        CancellationToken cancellationToken = default);
}

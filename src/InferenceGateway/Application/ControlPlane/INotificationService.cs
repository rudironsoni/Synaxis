namespace Synaxis.InferenceGateway.Application.ControlPlane;

/// <summary>
/// Service for sending notifications to clients.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a quota warning notification to clients.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="providerKey">The provider key</param>
    /// <param name="remainingQuota">The remaining quota</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendQuotaWarningAsync(
        string tenantId,
        string userId,
        string providerKey,
        int remainingQuota,
        CancellationToken cancellationToken = default);
}

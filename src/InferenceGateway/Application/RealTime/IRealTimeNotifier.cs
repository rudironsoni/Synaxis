namespace Synaxis.InferenceGateway.Application.RealTime;

/// <summary>
/// Interface for broadcasting real-time notifications to connected clients.
/// </summary>
public interface IRealTimeNotifier
{
    /// <summary>
    /// Notify about provider health status changes.
    /// </summary>
    Task NotifyProviderHealthChanged(Guid organizationId, ProviderHealthUpdate update);

    /// <summary>
    /// Notify when cost optimization is applied.
    /// </summary>
    Task NotifyCostOptimizationApplied(Guid organizationId, CostOptimizationResult result);

    /// <summary>
    /// Notify when a new model is discovered.
    /// </summary>
    Task NotifyModelDiscovered(Guid organizationId, ModelDiscoveryResult result);

    /// <summary>
    /// Notify about security alerts.
    /// </summary>
    Task NotifySecurityAlert(Guid organizationId, SecurityAlert alert);

    /// <summary>
    /// Notify about audit events.
    /// </summary>
    Task NotifyAuditEvent(Guid organizationId, AuditEvent @event);
}

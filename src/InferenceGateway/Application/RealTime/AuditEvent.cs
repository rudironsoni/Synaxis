namespace Synaxis.InferenceGateway.Application.RealTime;

/// <summary>
/// Real-time audit event notification.
/// </summary>
public record AuditEvent(
    Guid Id,
    string Action,
    string EntityType,
    string PerformedBy,
    DateTime PerformedAt
);

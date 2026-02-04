namespace Synaxis.InferenceGateway.Application.RealTime;

/// <summary>
/// Real-time security alert notification.
/// </summary>
public record SecurityAlert(
    Guid OrganizationId,
    string AlertType, // "weak_secret", "rate_limit_missing", etc.
    string Severity, // "critical", "warning", "info"
    string Message,
    DateTime DetectedAt
);

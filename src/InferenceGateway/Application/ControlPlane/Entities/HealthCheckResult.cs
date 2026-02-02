namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

/// <summary>
/// Results from provider health check validation.
/// </summary>
public record HealthCheckResult(
    bool IsHealthy,
    string? Endpoint,
    bool SupportsStreaming,
    bool SupportsChat,
    int? LatencyMs,
    string[] SupportedModels,
    string[] Errors
);

namespace Synaxis.InferenceGateway.Application.RealTime;

/// <summary>
/// Real-time update for provider health status changes.
/// </summary>
public record ProviderHealthUpdate(
    Guid ProviderId,
    string ProviderName,
    bool IsHealthy,
    decimal HealthScore,
    int AverageLatencyMs,
    DateTime CheckedAt
);

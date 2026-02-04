namespace Synaxis.InferenceGateway.Application.RealTime;

/// <summary>
/// Real-time notification when a new model is discovered.
/// </summary>
public record ModelDiscoveryResult(
    Guid ModelId,
    string CanonicalId,
    string DisplayName,
    string ProviderName,
    bool IsAvailableToOrganization
);

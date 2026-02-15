namespace Synaxis.Routing.Health;

/// <summary>
/// Represents the health status of a provider.
/// </summary>
public enum ProviderHealthStatus
{
    /// <summary>
    /// The provider health is unknown (no data).
    /// </summary>
    Unknown,

    /// <summary>
    /// The provider is healthy and functioning normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// The provider has some issues but is still functional.
    /// </summary>
    Warning,

    /// <summary>
    /// The provider is degraded with significant issues.
    /// </summary>
    Degraded,

    /// <summary>
    /// The provider is unhealthy and should not be used.
    /// </summary>
    Unhealthy
}

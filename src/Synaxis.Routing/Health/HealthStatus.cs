namespace Synaxis.Routing.Health;

/// <summary>
/// Represents the health status of a provider.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// The provider is healthy and functioning normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// The provider is degraded with significant issues.
    /// </summary>
    Degraded,

    /// <summary>
    /// The provider is unhealthy and should not be used.
    /// </summary>
    Unhealthy
}

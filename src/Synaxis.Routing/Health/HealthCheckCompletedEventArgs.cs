using Synaxis.Routing.Health;

namespace Synaxis.Routing.Health;

/// <summary>
/// Event arguments for health check completion.
/// </summary>
public class HealthCheckCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the health check result.
    /// </summary>
    public ProviderHealthCheckResult Result { get; set; } = new();
}

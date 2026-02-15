// <copyright file="HealthStatusChangedEventArgs.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

#nullable enable

namespace Synaxis.Routing.Health;

/// <summary>
/// Event arguments for health status changes.
/// </summary>
public class HealthStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous health status.
    /// </summary>
    public ProviderHealthStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new health status.
    /// </summary>
    public ProviderHealthStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the health check result.
    /// </summary>
    public ProviderHealthCheckResult? CheckResult { get; set; }
}

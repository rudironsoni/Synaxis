// <copyright file="HealthCheckCompletedEventArgs.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

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

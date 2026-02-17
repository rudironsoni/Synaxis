// <copyright file="HealthStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Infrastructure.ServiceDiscovery;

/// <summary>
/// Represents the health status of a service instance.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Health status is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Instance is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// Instance is unhealthy.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Instance is in a degraded state.
    /// </summary>
    Degraded,
}

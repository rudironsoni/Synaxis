// <copyright file="HealthCheckConfiguration.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

/// <summary>
/// Represents health check configuration for a container.
/// </summary>
public class HealthCheckConfiguration
{
    /// <summary>
    /// Gets or sets the HTTP path to check for health.
    /// </summary>
    public string? HttpPath { get; set; }

    /// <summary>
    /// Gets or sets the port to check for health.
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// Gets or sets the interval between health checks in seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the timeout for health checks in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the number of consecutive failures before marking as unhealthy.
    /// </summary>
    public int FailureThreshold { get; set; } = 3;
}

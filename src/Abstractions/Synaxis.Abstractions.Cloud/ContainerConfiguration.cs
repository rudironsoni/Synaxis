// <copyright file="ContainerConfiguration.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

using System.Collections.Generic;

/// <summary>
/// Represents configuration for a container deployment.
/// </summary>
public class ContainerConfiguration
{
    /// <summary>
    /// Gets or sets the environment variables for the container.
    /// </summary>
    public IDictionary<string, string> EnvironmentVariables { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the resource limits for the container.
    /// </summary>
    public ResourceLimits? ResourceLimits { get; set; }

    /// <summary>
    /// Gets or sets the port mappings for the container.
    /// </summary>
    public IDictionary<int, int> PortMappings { get; set; } =
        new Dictionary<int, int>();

    /// <summary>
    /// Gets or sets the health check configuration.
    /// </summary>
    public HealthCheckConfiguration? HealthCheck { get; set; }
}

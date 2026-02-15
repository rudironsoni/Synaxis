// <copyright file="DeploymentStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

/// <summary>
/// Represents the status of a deployment.
/// </summary>
public class DeploymentStatus
{
    /// <summary>
    /// Gets or sets the name of the deployment.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current state of the deployment.
    /// </summary>
    public DeploymentState State { get; set; }

    /// <summary>
    /// Gets or sets the number of running replicas.
    /// </summary>
    public int RunningReplicas { get; set; }

    /// <summary>
    /// Gets or sets the desired number of replicas.
    /// </summary>
    public int DesiredReplicas { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last update.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

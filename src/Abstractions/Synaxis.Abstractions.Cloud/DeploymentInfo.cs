// <copyright file="DeploymentInfo.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

/// <summary>
/// Represents information about a deployment.
/// </summary>
public class DeploymentInfo
{
    /// <summary>
    /// Gets or sets the name of the deployment.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container image.
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current state of the deployment.
    /// </summary>
    public DeploymentState State { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

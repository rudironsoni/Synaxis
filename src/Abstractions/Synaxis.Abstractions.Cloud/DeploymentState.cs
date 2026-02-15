// <copyright file="DeploymentState.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

/// <summary>
/// Represents the state of a deployment.
/// </summary>
public enum DeploymentState
{
    /// <summary>
    /// The deployment is being created.
    /// </summary>
    Pending,

    /// <summary>
    /// The deployment is running successfully.
    /// </summary>
    Running,

    /// <summary>
    /// The deployment has failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The deployment is being terminated.
    /// </summary>
    Terminating,

    /// <summary>
    /// The deployment has been terminated.
    /// </summary>
    Terminated,
}

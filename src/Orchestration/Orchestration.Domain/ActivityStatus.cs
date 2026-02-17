// <copyright file="ActivityStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain;

/// <summary>
/// Represents the status of a saga activity.
/// </summary>
public enum ActivityStatus
{
    /// <summary>
    /// Activity is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Activity is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Activity completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Activity failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Activity was compensated.
    /// </summary>
    Compensated,
}

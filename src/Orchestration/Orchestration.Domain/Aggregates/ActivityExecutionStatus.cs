// <copyright file="ActivityExecutionStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Represents the status of an activity execution.
/// </summary>
public enum ActivityExecutionStatus
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
    /// Activity was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Activity is waiting for external input.
    /// </summary>
    Waiting,
}

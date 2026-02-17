// <copyright file="BackgroundJobStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Represents the status of a background job.
/// </summary>
public enum BackgroundJobStatus
{
    /// <summary>
    /// Job is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Job is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Job failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Job is scheduled for retry.
    /// </summary>
    Retrying,

    /// <summary>
    /// Job was cancelled.
    /// </summary>
    Cancelled,
}

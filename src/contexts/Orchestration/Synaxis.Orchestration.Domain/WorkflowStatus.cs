// <copyright file="WorkflowStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain;

/// <summary>
/// Represents the status of an orchestration workflow.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// Workflow is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Workflow is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Workflow completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Workflow failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Workflow was compensated due to saga failure.
    /// </summary>
    Compensated,

    /// <summary>
    /// Workflow was cancelled.
    /// </summary>
    Cancelled,
}

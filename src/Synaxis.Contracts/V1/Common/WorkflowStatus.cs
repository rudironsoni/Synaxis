namespace Synaxis.Contracts.V1.Common;

/// <summary>
/// Represents the status of a workflow.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// Workflow is pending and has not started yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Workflow is currently in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Workflow has completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Workflow has failed with an error.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Workflow was cancelled before completion.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Workflow is paused and can be resumed.
    /// </summary>
    Paused = 5
}

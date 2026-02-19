namespace Synaxis.Contracts.V2.Common;

/// <summary>
/// Represents the status of a workflow (V2).
/// </summary>
/// <remarks>
/// V2: No breaking changes, added 'Scheduled' status for future execution.
/// </remarks>
public enum WorkflowStatus
{
    /// <summary>
    /// Workflow is scheduled for future execution.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Workflow is pending and has not started yet.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Workflow is currently in progress.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Workflow has completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Workflow has failed with an error.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Workflow was cancelled before completion.
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Workflow is paused and can be resumed.
    /// </summary>
    Paused = 6
}

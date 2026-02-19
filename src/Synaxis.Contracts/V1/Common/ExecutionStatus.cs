namespace Synaxis.Contracts.V1.Common;

/// <summary>
/// Represents the status of an agent execution.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>
    /// Execution is pending and has not started yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Execution is currently in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Execution has completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Execution has failed with an error.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Execution was cancelled before completion.
    /// </summary>
    Cancelled = 4
}

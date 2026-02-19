namespace Synaxis.Contracts.V2.Common;

/// <summary>
/// Represents the status of an agent execution (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Renamed 'InProgress' to 'Running' for consistency with agent status
/// - Added 'Paused' status for suspending execution
/// - Added 'Timeout' status for explicit timeout handling
/// </remarks>
public enum ExecutionStatus
{
    /// <summary>
    /// Execution is pending and has not started yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Execution is currently running.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Execution has been paused and can be resumed.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Execution has completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Execution has failed with an error.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Execution was cancelled before completion.
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Execution timed out.
    /// </summary>
    Timeout = 6
}

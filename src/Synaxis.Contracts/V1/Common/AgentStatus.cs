namespace Synaxis.Contracts.V1.Common;

/// <summary>
/// Represents the status of an agent in the system.
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// Agent is being created and not yet ready.
    /// </summary>
    Creating = 0,

    /// <summary>
    /// Agent is idle and ready to execute tasks.
    /// </summary>
    Idle = 1,

    /// <summary>
    /// Agent is currently executing a task.
    /// </summary>
    Executing = 2,

    /// <summary>
    /// Agent has encountered an error and is unavailable.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Agent has been disabled by an administrator.
    /// </summary>
    Disabled = 4
}

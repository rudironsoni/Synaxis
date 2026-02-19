namespace Synaxis.Contracts.V2.Common;

/// <summary>
/// Represents the status of an agent in the system (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Renamed 'Creating' to 'Provisioning' to better reflect cloud-native terminology
/// - Split 'Executing' into 'Running' and 'Processing' for finer granularity
/// - Added 'Draining' status for graceful shutdown
/// </remarks>
public enum AgentStatus
{
    /// <summary>
    /// Agent is being provisioned and not yet ready.
    /// </summary>
    Provisioning = 0,

    /// <summary>
    /// Agent is idle and ready to execute tasks.
    /// </summary>
    Idle = 1,

    /// <summary>
    /// Agent is running but waiting for input.
    /// </summary>
    Running = 2,

    /// <summary>
    /// Agent is actively processing a task.
    /// </summary>
    Processing = 3,

    /// <summary>
    /// Agent is draining and will not accept new tasks.
    /// </summary>
    Draining = 4,

    /// <summary>
    /// Agent has encountered an error and is unavailable.
    /// </summary>
    Error = 5,

    /// <summary>
    /// Agent has been disabled by an administrator.
    /// </summary>
    Disabled = 6
}

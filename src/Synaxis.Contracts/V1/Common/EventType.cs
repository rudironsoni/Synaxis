namespace Synaxis.Contracts.V1.Common;

/// <summary>
/// Represents the type of domain event.
/// </summary>
public enum EventType
{
    /// <summary>
    /// User-related event.
    /// </summary>
    User = 0,

    /// <summary>
    /// Agent-related event.
    /// </summary>
    Agent = 1,

    /// <summary>
    /// Workflow-related event.
    /// </summary>
    Workflow = 2,

    /// <summary>
    /// Execution-related event.
    /// </summary>
    Execution = 3
}

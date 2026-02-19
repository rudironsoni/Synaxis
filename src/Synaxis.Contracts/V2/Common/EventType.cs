namespace Synaxis.Contracts.V2.Common;

/// <summary>
/// Represents the type of domain event (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Values are now explicit for forward compatibility
/// - Added 'System' event type for platform-level events
/// </remarks>
public enum EventType
{
    /// <summary>
    /// User-related event.
    /// </summary>
    User = 100,

    /// <summary>
    /// Agent-related event.
    /// </summary>
    Agent = 200,

    /// <summary>
    /// Workflow-related event.
    /// </summary>
    Workflow = 300,

    /// <summary>
    /// Execution-related event.
    /// </summary>
    Execution = 400,

    /// <summary>
    /// System/platform-level event.
    /// </summary>
    System = 500
}

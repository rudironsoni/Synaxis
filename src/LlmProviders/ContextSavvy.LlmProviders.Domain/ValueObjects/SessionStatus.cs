namespace ContextSavvy.LlmProviders.Domain.ValueObjects;

/// <summary>
/// Represents the current lifecycle status of a provider session.
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// The session is being initialized.
    /// </summary>
    Initializing,

    /// <summary>
    /// The session is ready to accept requests.
    /// </summary>
    Ready,

    /// <summary>
    /// The session is currently processing a request.
    /// </summary>
    Busy,

    /// <summary>
    /// The session has encountered an error and may need recovery.
    /// </summary>
    Error,

    /// <summary>
    /// The session has been disposed and is no longer usable.
    /// </summary>
    Disposed
}

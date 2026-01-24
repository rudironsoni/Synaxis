using Synaplexer.Domain.ValueObjects;

namespace Synaplexer.Domain.Entities;

/// <summary>
/// Represents an active session with an LLM provider.
/// </summary>
public class ProviderSession
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Identifier of the provider associated with this session.
    /// </summary>
    public string ProviderId { get; private set; }

    /// <summary>
    /// Current status of the session.
    /// </summary>
    public SessionStatus Status { get; private set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the last activity occurred on this session.
    /// </summary>
    public DateTime LastActivityAt { get; private set; }

    /// <summary>
    /// The number of consecutive errors encountered by this session.
    /// </summary>
    public int ErrorCount { get; private set; }

    private const int MaxErrorThreshold = 3;

    public ProviderSession(Guid id, string providerId)
    {
        Id = id;
        ProviderId = providerId;
        Status = SessionStatus.Initializing;
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        ErrorCount = 0;
    }

    /// <summary>
    /// Marks the session as active and updates the last activity timestamp.
    /// </summary>
    public void MarkActive()
    {
        Status = SessionStatus.Ready;
        LastActivityAt = DateTime.UtcNow;
        ErrorCount = 0;
    }

    /// <summary>
    /// Increments the error count and updates the session status if necessary.
    /// </summary>
    public void MarkError()
    {
        ErrorCount++;
        LastActivityAt = DateTime.UtcNow;
        
        if (ErrorCount >= MaxErrorThreshold)
        {
            Status = SessionStatus.Error;
        }
    }

    /// <summary>
    /// Determines if the session is considered healthy based on status and error count.
    /// </summary>
    /// <returns>True if the session is healthy; otherwise, false.</returns>
    public bool IsHealthy()
    {
        return Status != SessionStatus.Error && 
               Status != SessionStatus.Disposed && 
               ErrorCount < MaxErrorThreshold;
    }
}

using ContextSavvy.LlmProviders.Domain.Entities;
using ContextSavvy.LlmProviders.Domain.ValueObjects;

namespace ContextSavvy.LlmProviders.Domain.Aggregates;

/// <summary>
/// Aggregate root that manages a pool of LLM provider sessions.
/// </summary>
public class ProviderPool
{
    private readonly List<ProviderSession> _sessions = new();

    /// <summary>
    /// Unique identifier for the provider pool.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The type of provider this pool manages.
    /// </summary>
    public ProviderType Provider { get; private set; }

    /// <summary>
    /// Read-only collection of sessions in the pool.
    /// </summary>
    public IReadOnlyCollection<ProviderSession> Sessions => _sessions.AsReadOnly();

    public ProviderPool(Guid id, ProviderType provider)
    {
        Id = id;
        Provider = provider;
    }

    /// <summary>
    /// Adds a new session to the pool.
    /// </summary>
    /// <param name="session">The session to add.</param>
    public void AddSession(ProviderSession session)
    {
        if (!_sessions.Any(s => s.Id == session.Id))
        {
            _sessions.Add(session);
        }
    }

    /// <summary>
    /// Retrieves an available and healthy session from the pool.
    /// </summary>
    /// <returns>A healthy ProviderSession if available; otherwise, null.</returns>
    public ProviderSession? GetAvailableSession()
    {
        return _sessions.FirstOrDefault(s => s.IsHealthy() && s.Status == SessionStatus.Ready);
    }

    /// <summary>
    /// Returns a session to the pool, marking it as ready if it's healthy.
    /// </summary>
    /// <param name="sessionId">The ID of the session being returned.</param>
    public void ReturnSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null && session.IsHealthy())
        {
            session.MarkActive();
        }
    }

    /// <summary>
    /// Gets the count of currently healthy sessions in the pool.
    /// </summary>
    /// <returns>The number of healthy sessions.</returns>
    public int GetHealthySessionCount()
    {
        return _sessions.Count(s => s.IsHealthy());
    }
}

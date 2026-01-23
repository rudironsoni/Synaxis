using ContextSavvy.LlmProviders.Domain.Entities;

namespace ContextSavvy.LlmProviders.Domain.Interfaces;

/// <summary>
/// Repository interface for managing ProviderSession entities.
/// </summary>
public interface IProviderSessionRepository
{
    /// <summary>
    /// Retrieves a session by its unique identifier.
    /// </summary>
    Task<ProviderSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active sessions for a specific provider.
    /// </summary>
    Task<IEnumerable<ProviderSession>> GetActiveByProviderIdAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new session to the repository.
    /// </summary>
    Task AddAsync(ProviderSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing session in the repository.
    /// </summary>
    Task UpdateAsync(ProviderSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session by its unique identifier.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

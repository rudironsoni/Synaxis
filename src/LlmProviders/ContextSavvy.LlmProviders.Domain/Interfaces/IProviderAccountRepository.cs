using ContextSavvy.LlmProviders.Domain.Entities;
using ContextSavvy.LlmProviders.Domain.ValueObjects;

namespace ContextSavvy.LlmProviders.Domain.Interfaces;

/// <summary>
/// Repository interface for managing ProviderAccount entities.
/// </summary>
public interface IProviderAccountRepository
{
    /// <summary>
    /// Retrieves an account by its unique identifier.
    /// </summary>
    Task<ProviderAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all accounts for a specific provider type.
    /// </summary>
    Task<IEnumerable<ProviderAccount>> GetByProviderTypeAsync(ProviderType providerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active and available accounts for a specific provider type.
    /// </summary>
    Task<IEnumerable<ProviderAccount>> GetAvailableByProviderTypeAsync(ProviderType providerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new account to the repository.
    /// </summary>
    Task AddAsync(ProviderAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing account in the repository.
    /// </summary>
    Task UpdateAsync(ProviderAccount account, CancellationToken cancellationToken = default);
}

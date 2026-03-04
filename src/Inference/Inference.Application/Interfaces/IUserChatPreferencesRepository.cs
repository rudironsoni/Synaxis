// <copyright file="IUserChatPreferencesRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Interfaces;

using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Repository interface for user chat preferences operations.
/// </summary>
public interface IUserChatPreferencesRepository
{
    /// <summary>
    /// Gets preferences by its identifier.
    /// </summary>
    /// <param name="id">The preferences identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The preferences if found; otherwise null.</returns>
    Task<UserChatPreferences?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets preferences by user identifier.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The preferences if found; otherwise null.</returns>
    Task<UserChatPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets preferences by tenant and user identifiers.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The preferences if found; otherwise null.</returns>
    Task<UserChatPreferences?> GetByTenantAndUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all preferences for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of preferences.</returns>
    Task<IReadOnlyList<UserChatPreferences>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds new preferences.
    /// </summary>
    /// <param name="preferences">The preferences.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(UserChatPreferences preferences, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates existing preferences.
    /// </summary>
    /// <param name="preferences">The preferences.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(UserChatPreferences preferences, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if preferences exist for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if preferences exist; otherwise false.</returns>
    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

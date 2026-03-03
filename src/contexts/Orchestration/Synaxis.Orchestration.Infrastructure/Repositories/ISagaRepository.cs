// <copyright file="ISagaRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure.Repositories;

using Synaxis.Orchestration.Domain;
using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Repository for saga aggregates.
/// </summary>
public interface ISagaRepository
{
    /// <summary>
    /// Gets a saga by ID.
    /// </summary>
    /// <param name="id">The saga identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saga if found; otherwise, <see langword="null"/>.</returns>
    Task<Saga?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a saga.
    /// </summary>
    /// <param name="saga">The saga to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAsync(Saga saga, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas by tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of sagas for the specified tenant.</returns>
    Task<IReadOnlyList<Saga>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas by status.
    /// </summary>
    /// <param name="status">The saga status.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of sagas with the specified status.</returns>
    Task<IReadOnlyList<Saga>> GetByStatusAsync(SagaStatus status, CancellationToken cancellationToken = default);
}

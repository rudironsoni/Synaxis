// <copyright file="IInferenceRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.Persistence;

using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Repository interface for inference request operations.
/// </summary>
public interface IInferenceRepository
{
    /// <summary>
    /// Gets an inference request by its identifier.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The inference request if found; otherwise null.</returns>
    Task<InferenceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inference requests by tenant identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of inference requests.</returns>
    Task<IReadOnlyList<InferenceRequest>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inference requests by user identifier.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of inference requests.</returns>
    Task<IReadOnlyList<InferenceRequest>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending inference requests.
    /// </summary>
    /// <param name="limit">The maximum number of requests to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of pending inference requests.</returns>
    Task<IReadOnlyList<InferenceRequest>> GetPendingAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inference requests by status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="tenantId">The optional tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of inference requests.</returns>
    Task<IReadOnlyList<InferenceRequest>> GetByStatusAsync(
        InferenceStatus status,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new inference request.
    /// </summary>
    /// <param name="request">The inference request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(InferenceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing inference request.
    /// </summary>
    /// <param name="request">The inference request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(InferenceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inference requests within a date range.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of inference requests.</returns>
    Task<IReadOnlyList<InferenceRequest>> GetByDateRangeAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}

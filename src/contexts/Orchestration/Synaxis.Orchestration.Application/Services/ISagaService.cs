// <copyright file="ISagaService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Application.Services;

using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Service for managing saga lifecycle.
/// </summary>
public interface ISagaService
{
    /// <summary>
    /// Creates a new saga.
    /// </summary>
    /// <param name="name">The name of the saga.</param>
    /// <param name="description">The description of the saga.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created saga.</returns>
    Task<Saga> CreateSagaAsync(
        string name,
        string? description,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an activity to a saga.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="name">The name of the activity.</param>
    /// <param name="sequence">The sequence number of the activity.</param>
    /// <param name="workflowDefinitionId">The workflow definition identifier.</param>
    /// <param name="compensationActivityId">The compensation activity identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddActivityAsync(
        Guid sagaId,
        string name,
        int sequence,
        Guid workflowDefinitionId,
        Guid? compensationActivityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a saga.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an activity in a saga.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="activityId">The activity identifier.</param>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompleteActivityAsync(
        Guid sagaId,
        Guid activityId,
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a saga.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompleteSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fails a saga.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="failedActivityId">The identifier of the failed activity.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FailSagaAsync(
        Guid sagaId,
        Guid failedActivityId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a saga by ID.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saga if found; otherwise, null.</returns>
    Task<Saga?> GetSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas by tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of sagas for the tenant.</returns>
    Task<IReadOnlyList<Saga>> GetSagasByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

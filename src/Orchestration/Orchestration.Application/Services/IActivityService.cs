// <copyright file="IActivityService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Application.Services;

using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Service for managing activity execution.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Creates a new activity.
    /// </summary>
    /// <param name="name">The name of the activity.</param>
    /// <param name="activityType">The type of the activity.</param>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="inputData">The input data for the activity.</param>
    /// <param name="executionContext">The execution context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created activity.</returns>
    Task<Activity> CreateActivityAsync(
        string name,
        string activityType,
        Guid? workflowId,
        Guid? sagaId,
        Guid tenantId,
        string? inputData,
        string? executionContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an activity.
    /// </summary>
    /// <param name="activityId">The activity identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartActivityAsync(Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an activity.
    /// </summary>
    /// <param name="activityId">The activity identifier.</param>
    /// <param name="outputData">The output data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompleteActivityAsync(Guid activityId, string outputData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fails an activity.
    /// </summary>
    /// <param name="activityId">The activity identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FailActivityAsync(Guid activityId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries an activity.
    /// </summary>
    /// <param name="activityId">The activity identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RetryActivityAsync(Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an activity by ID.
    /// </summary>
    /// <param name="activityId">The activity identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activity if found; otherwise, null.</returns>
    Task<Activity?> GetActivityAsync(Guid activityId, CancellationToken cancellationToken = default);
}

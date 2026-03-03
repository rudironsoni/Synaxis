// <copyright file="IActivityRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure.Repositories;

using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Repository for activity aggregates.
/// </summary>
public interface IActivityRepository
{
    /// <summary>
    /// Gets an activity by ID.
    /// </summary>
    /// <param name="id">The activity identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activity if found; otherwise, <see langword="null"/>.</returns>
    Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an activity.
    /// </summary>
    /// <param name="activity">The activity to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAsync(Activity activity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities by workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of activities associated with the specified workflow.</returns>
    Task<IReadOnlyList<Activity>> GetByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities by saga.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of activities associated with the specified saga.</returns>
    Task<IReadOnlyList<Activity>> GetBySagaAsync(Guid sagaId, CancellationToken cancellationToken = default);
}

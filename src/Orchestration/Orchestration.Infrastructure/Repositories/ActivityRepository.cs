// <copyright file="ActivityRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure.Repositories;

using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Implementation of activity repository using event sourcing.
/// </summary>
public class ActivityRepository : IActivityRepository
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<ActivityRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityRepository"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="logger">The logger.</param>
    public ActivityRepository(IEventStore eventStore, ILogger<ActivityRepository> logger)
    {
        this._eventStore = eventStore;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var events = await this._eventStore.ReadStreamAsync(id.ToString(), cancellationToken).ConfigureAwait(false);
        if (events.Count == 0)
        {
            return null;
        }

        var activity = new Activity();
        activity.LoadFromHistory(events);

        return activity;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        var uncommittedEvents = activity.GetUncommittedEvents();
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var expectedVersion = activity.Version - uncommittedEvents.Count;

        await this._eventStore.AppendAsync(
            activity.Id.ToString(),
            uncommittedEvents,
            expectedVersion,
            cancellationToken).ConfigureAwait(false);

        activity.MarkAsCommitted();

        this._logger.LogDebug(
            "Saved activity {ActivityId} with {EventCount} events",
            activity.Id,
            uncommittedEvents.Count);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Activity>> GetByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Activity>>(new List<Activity>());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Activity>> GetBySagaAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Activity>>(new List<Activity>());
    }
}

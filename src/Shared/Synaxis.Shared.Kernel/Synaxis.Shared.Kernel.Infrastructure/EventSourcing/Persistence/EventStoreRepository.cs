// <copyright file="EventStoreRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing.Aggregates;
using Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing.Publishing;
using Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing.Serialization;

/// <summary>
/// Repository for event-sourced aggregates that handles persistence and publishing.
/// </summary>
/// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
public class EventStoreRepository<TAggregate>
    where TAggregate : EventSourcedAggregate, new()
{
    private readonly IEventStore eventStore;
    private readonly IDomainEventPublisher? eventPublisher;
    private readonly ISnapshotStore? snapshotStore;
    private readonly int snapshotThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreRepository{TAggregate}"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="serializer">The event serializer.</param>
    /// <param name="publisher">Optional domain event publisher.</param>
    /// <param name="snapshotStore">Optional snapshot store.</param>
    /// <param name="snapshotThreshold">Number of events before creating a snapshot.</param>
    public EventStoreRepository(
        IEventStore eventStore,
        IEventSerializer serializer,
        IDomainEventPublisher? publisher = null,
        ISnapshotStore? snapshotStore = null,
        int snapshotThreshold = 100)
    {
        this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        this.eventPublisher = publisher;
        this.snapshotStore = snapshotStore;
        this.snapshotThreshold = snapshotThreshold;
    }

    /// <summary>
    /// Gets an aggregate by its ID, loading from snapshot if available.
    /// </summary>
    /// <param name="id">The aggregate ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate, or null if not found.</returns>
    public async Task<TAggregate?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID cannot be null or empty", nameof(id));
        }

        var aggregate = new TAggregate();
        var fromVersion = 0L;

        // Try to load from snapshot if supported
        if (aggregate is IAggregateSnapshot snapshotAggregate && this.snapshotStore is not null)
        {
            var snapshotType = snapshotAggregate.GetSnapshotType();
            var getSnapshotMethod = typeof(ISnapshotStore).GetMethod(nameof(ISnapshotStore.GetSnapshotAsync))?
                .MakeGenericMethod(snapshotType);

            if (getSnapshotMethod is not null)
            {
                var task = getSnapshotMethod.Invoke(this.snapshotStore, new object[] { id, cancellationToken });
                if (task is Task taskObj)
                {
                    await taskObj.ConfigureAwait(false);
                    var result = taskObj.GetType().GetProperty("Result")?.GetValue(taskObj);

                    if (result is not null)
                    {
                        var stateProperty = result.GetType().GetProperty("State");
                        var versionProperty = result.GetType().GetProperty("Version");

                        if (stateProperty is not null && versionProperty is not null)
                        {
                            var state = stateProperty.GetValue(result);
                            var version = (long)(versionProperty.GetValue(result) ?? 0L);

                            if (state is not null)
                            {
                                snapshotAggregate.RestoreFromSnapshot(state);
                                fromVersion = version + 1;
                            }
                        }
                    }
                }
            }
        }

        // Load events after the snapshot version
        var events = new List<object>();
        await foreach (var envelope in this.eventStore.ReadAsync(id, fromVersion, cancellationToken))
        {
            events.Add(envelope.EventData);
        }

        if (events.Count == 0 && fromVersion == 0)
        {
            return null;
        }

        aggregate.LoadFromHistory(events);
        return aggregate;
    }

    /// <summary>
    /// Saves an aggregate by persisting uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate to save.</param>
    /// <param name="metadata">Optional event metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SaveAsync(
        TAggregate aggregate,
        EventMetadata? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (aggregate is null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        var uncommittedEvents = aggregate.UncommittedEvents;
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var eventEnvelopes = new List<IEventEnvelope>();
        var nextVersion = aggregate.OriginalVersion + 1;

        foreach (var @event in uncommittedEvents)
        {
            var envelope = new EventEnvelope<object>(
                Guid.NewGuid(),
                aggregate.Id,
                nextVersion++,
                0, // Global position assigned by database
                @event,
                metadata,
                DateTime.UtcNow);

            eventEnvelopes.Add(envelope);
        }

        // Append events to store with optimistic concurrency
        await this.eventStore.AppendAsync(
            aggregate.Id,
            aggregate.OriginalVersion,
            eventEnvelopes,
            cancellationToken);

        // Mark events as committed
        aggregate.MarkCommitted();

        // Create snapshot if threshold reached and supported
        if (this.snapshotStore is not null && aggregate is IAggregateSnapshot snapshotAggregate)
        {
            var eventsSinceSnapshot = aggregate.Version - (aggregate.OriginalVersion - uncommittedEvents.Count);
            if (eventsSinceSnapshot >= this.snapshotThreshold)
            {
                var snapshot = snapshotAggregate.CreateSnapshot();
                var saveSnapshotMethod = typeof(ISnapshotStore).GetMethod(nameof(ISnapshotStore.SaveSnapshotAsync))?
                    .MakeGenericMethod(snapshot.GetType());

                if (saveSnapshotMethod is not null)
                {
                    var task = saveSnapshotMethod.Invoke(
                        this.snapshotStore,
                        new object?[] { aggregate.Id, aggregate.Version, snapshot, cancellationToken });

                    if (task is Task taskObj)
                    {
                        await taskObj.ConfigureAwait(false);
                    }
                }
            }
        }

        // Publish events to message bus
        if (this.eventPublisher is not null)
        {
            var notifications = uncommittedEvents
                .OfType<Synaxis.Shared.Kernel.Application.Commands.INotification>()
                .ToList();

            if (notifications.Count > 0)
            {
                await this.eventPublisher.PublishAllAsync(notifications, cancellationToken);
            }
        }
    }
}

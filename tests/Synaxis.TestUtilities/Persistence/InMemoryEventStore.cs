// <copyright file="InMemoryEventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Persistence;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// An in-memory implementation of <see cref="IEventStore"/> for testing purposes.
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread-safe storage.
/// </summary>
public sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<string, List<IDomainEvent>> _streams = new();
    private readonly ConcurrentDictionary<Guid, List<IDomainEvent>> _aggregateEvents = new();
    private readonly ConcurrentBag<IDomainEvent> _allEvents = new();
    private readonly List<Func<IDomainEvent, Task>> _subscribers = new();
    private readonly object _subscriberLock = new();
    private int _eventCounter;

    /// <summary>
    /// Appends a sequence of events to a stream with optimistic concurrency control.
    /// </summary>
    /// <param name="streamId">The unique identifier of the event stream.</param>
    /// <param name="events">The events to append to the stream.</param>
    /// <param name="expectedVersion">The expected version of the stream for optimistic concurrency.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="events"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="streamId"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when optimistic concurrency check fails.</exception>
    public Task AppendAsync(
        string streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamId);
        ArgumentNullException.ThrowIfNull(events);

        cancellationToken.ThrowIfCancellationRequested();

        var eventList = events.ToList();

        if (eventList.Count == 0)
        {
            return Task.CompletedTask;
        }

        var streamEvents = _streams.GetOrAdd(streamId, _ => new List<IDomainEvent>());

        lock (streamEvents)
        {
            if (streamEvents.Count != expectedVersion)
            {
                throw new InvalidOperationException(
                    $"Expected version {expectedVersion} but stream '{streamId}' is at version {streamEvents.Count}.");
            }

            foreach (var @event in eventList)
            {
                streamEvents.Add(@event);
                _allEvents.Add(@event);

                if (@event is IAggregateDomainEvent aggregateEvent &&
                    Guid.TryParse(aggregateEvent.AggregateId, out var aggregateId))
                {
                    _aggregateEvents.AddOrUpdate(
                        aggregateId,
                        [aggregateEvent],
                        (_, list) =>
                        {
                            list.Add(aggregateEvent);
                            return list;
                        });
                }

                Interlocked.Increment(ref _eventCounter);
            }
        }

        _ = Task.Run(async () =>
        {
            List<Func<IDomainEvent, Task>> handlers;
            lock (_subscriberLock)
            {
                handlers = new List<Func<IDomainEvent, Task>>(_subscribers);
            }

            foreach (var @event in eventList)
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler(@event).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore subscriber exceptions in tests
                    }
                }
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads events from a stream within a version range.
    /// </summary>
    /// <param name="streamId">The unique identifier of the event stream.</param>
    /// <param name="fromVersion">The starting version to read from (inclusive).</param>
    /// <param name="toVersion">The ending version to read to (inclusive).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of events within the specified version range.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="streamId"/> is null or empty.</exception>
    public Task<IReadOnlyList<IDomainEvent>> ReadAsync(
        string streamId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamId);
        cancellationToken.ThrowIfCancellationRequested();

        if (_streams.TryGetValue(streamId, out var streamEvents))
        {
            lock (streamEvents)
            {
                var events = streamEvents
                    .Skip(fromVersion)
                    .Take(toVersion - fromVersion + 1)
                    .ToImmutableList();

                return Task.FromResult<IReadOnlyList<IDomainEvent>>(events);
            }
        }

        return Task.FromResult<IReadOnlyList<IDomainEvent>>(ImmutableList<IDomainEvent>.Empty);
    }

    /// <summary>
    /// Reads all events from a stream.
    /// </summary>
    /// <param name="streamId">The unique identifier of the event stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of all events in the stream.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="streamId"/> is null or empty.</exception>
    public Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamId);
        cancellationToken.ThrowIfCancellationRequested();

        if (_streams.TryGetValue(streamId, out var streamEvents))
        {
            lock (streamEvents)
            {
                return Task.FromResult<IReadOnlyList<IDomainEvent>>(streamEvents.ToImmutableList());
            }
        }

        return Task.FromResult<IReadOnlyList<IDomainEvent>>(ImmutableList<IDomainEvent>.Empty);
    }

    /// <summary>
    /// Deletes an event stream.
    /// </summary>
    /// <param name="streamId">The unique identifier of the event stream to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="streamId"/> is null or empty.</exception>
    public Task DeleteAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamId);
        cancellationToken.ThrowIfCancellationRequested();

        _streams.TryRemove(streamId, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads all events across all streams.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of all events.</returns>
    public Task<IReadOnlyList<IDomainEvent>> ReadAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<IDomainEvent>>(_allEvents.ToImmutableList());
    }

    /// <summary>
    /// Subscribes to new events as they are appended to the store.
    /// </summary>
    /// <param name="handler">The handler to process received events.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
    public Task SubscribeToAllAsync(
        Func<IDomainEvent, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_subscriberLock)
        {
            _subscribers.Add(handler);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the total number of events stored across all streams.
    /// </summary>
    /// <returns>The total event count.</returns>
    public int GetEventCount() => Interlocked.CompareExchange(ref _eventCounter, 0, 0);

    /// <summary>
    /// Clears all events from the store.
    /// </summary>
    public void Clear()
    {
        _streams.Clear();
        _aggregateEvents.Clear();
        while (_allEvents.TryTake(out _))
        {
        }

        lock (_subscriberLock)
        {
            _subscribers.Clear();
        }

        Interlocked.Exchange(ref _eventCounter, 0);
    }

    /// <summary>
    /// Gets events for a specific aggregate by its identifier.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <returns>A read-only list of events for the aggregate.</returns>
    public IReadOnlyList<IDomainEvent> GetAggregateEvents(Guid aggregateId)
    {
        return _aggregateEvents.TryGetValue(aggregateId, out var events)
            ? events.ToImmutableList()
            : ImmutableList<IDomainEvent>.Empty;
    }

    /// <summary>
    /// Gets all stream identifiers.
    /// </summary>
    /// <returns>A collection of stream identifiers.</returns>
    public IEnumerable<string> GetStreamIds() => _streams.Keys.ToImmutableList();

    /// <summary>
    /// Checks if a stream exists.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>True if the stream exists; otherwise, false.</returns>
    public bool StreamExists(string streamId) => _streams.ContainsKey(streamId);
}

/// <summary>
/// Interface for domain events that have an aggregate identifier.
/// </summary>
public interface IAggregateDomainEvent : IDomainEvent
{
    /// <summary>
    /// Gets the aggregate identifier.
    /// </summary>
    string AggregateId { get; }
}

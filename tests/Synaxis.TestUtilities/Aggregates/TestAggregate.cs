// <copyright file="TestAggregate.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Aggregates;

using System.Collections.Immutable;
using Synaxis.Abstractions.Cloud;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Base class for test aggregates providing helper methods for event application and state verification.
/// </summary>
public abstract class TestAggregate : AggregateRoot
{
    private readonly List<IDomainEvent> _appliedEvents = new();
    private object? _lastSnapshot;
    private int _snapshotVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAggregate"/> class.
    /// </summary>
    protected TestAggregate()
    {
    }

    /// <summary>
    /// Gets the event history for this aggregate.
    /// </summary>
    public IReadOnlyList<IDomainEvent> EventHistory => _appliedEvents.AsReadOnly();

    /// <summary>
    /// Gets the last snapshot taken of this aggregate.
    /// </summary>
    public object? LastSnapshot => _lastSnapshot;

    /// <summary>
    /// Gets the version at which the last snapshot was taken.
    /// </summary>
    public int SnapshotVersion => _snapshotVersion;

    /// <summary>
    /// Gets the number of events in the history.
    /// </summary>
    public int EventCount => _appliedEvents.Count;

    /// <summary>
    /// Gets a value indicating whether a snapshot exists.
    /// </summary>
    public bool HasSnapshot => _lastSnapshot != null;

    /// <summary>
    /// Applies an event and tracks it in the event history.
    /// This method is intended for testing purposes to build up aggregate history.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="event">The event to apply.</param>
    public void ApplyEvent<TEvent>(TEvent @event)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        _appliedEvents.Add(@event);

        // Apply the event to the aggregate state
        Apply(@event);
    }

    /// <summary>
    /// Applies multiple events and tracks them in the event history.
    /// </summary>
    /// <param name="events">The events to apply.</param>
    public void ApplyEvents(IEnumerable<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var @event in events)
        {
            ApplyEvent(@event);
        }
    }

    /// <summary>
    /// Loads the aggregate state from a history of events.
    /// </summary>
    /// <param name="events">The events to load into the aggregate.</param>
    public new void LoadFromHistory(IEnumerable<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        _appliedEvents.AddRange(eventList);

        base.LoadFromHistory(eventList);
    }

    /// <summary>
    /// Takes a snapshot of the current aggregate state.
    /// </summary>
    /// <returns>The snapshot representing the current state.</returns>
    public object TakeSnapshot()
    {
        _lastSnapshot = CreateSnapshot();
        _snapshotVersion = Version;
        return _lastSnapshot;
    }

    /// <summary>
    /// Restores the aggregate from a snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    public void RestoreFromSnapshot(object snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        ApplySnapshot(snapshot);
        _lastSnapshot = snapshot;
    }

    /// <summary>
    /// Clears the event history.
    /// </summary>
    public void ClearHistory()
    {
        _appliedEvents.Clear();
    }

    /// <summary>
    /// Checks if a specific event type exists in the history.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check for.</typeparam>
    /// <returns>True if the event type exists in history; otherwise, false.</returns>
    public bool ContainsEvent<TEvent>()
        where TEvent : IDomainEvent
    {
        return _appliedEvents.Any(e => e is TEvent);
    }

    /// <summary>
    /// Checks if a specific event type exists in the history matching a predicate.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check for.</typeparam>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>True if a matching event exists; otherwise, false.</returns>
    public bool ContainsEvent<TEvent>(Func<TEvent, bool> predicate)
        where TEvent : IDomainEvent
    {
        return _appliedEvents
            .OfType<TEvent>()
            .Any(predicate);
    }

    /// <summary>
    /// Gets all events of a specific type from the history.
    /// </summary>
    /// <typeparam name="TEvent">The event type to retrieve.</typeparam>
    /// <returns>A list of events of the specified type.</returns>
    public IReadOnlyList<TEvent> GetEvents<TEvent>()
        where TEvent : IDomainEvent
    {
        return _appliedEvents
            .OfType<TEvent>()
            .ToImmutableList();
    }

    /// <summary>
    /// Gets the last event of a specific type from the history.
    /// </summary>
    /// <typeparam name="TEvent">The event type to retrieve.</typeparam>
    /// <returns>The last event of the specified type, or null if not found.</returns>
    public TEvent? GetLastEvent<TEvent>()
        where TEvent : IDomainEvent
    {
        return _appliedEvents
            .OfType<TEvent>()
            .LastOrDefault();
    }

    /// <summary>
    /// Verifies the aggregate is in the expected state.
    /// </summary>
    /// <param name="expectedVersion">The expected version.</param>
    /// <returns>True if the aggregate version matches; otherwise, false.</returns>
    public bool IsAtVersion(int expectedVersion) => Version == expectedVersion;

    /// <summary>
    /// Verifies the aggregate identifier matches the expected value.
    /// </summary>
    /// <param name="expectedId">The expected identifier.</param>
    /// <returns>True if the identifier matches; otherwise, false.</returns>
    public bool HasId(string expectedId) => Id == expectedId;

    /// <summary>
    /// Creates a snapshot of the current aggregate state.
    /// </summary>
    /// <returns>The snapshot object.</returns>
    protected abstract object CreateSnapshot();

    /// <summary>
    /// Applies a snapshot to restore aggregate state.
    /// </summary>
    /// <param name="snapshot">The snapshot to apply.</param>
    protected abstract void ApplySnapshot(object snapshot);
}

/// <summary>
/// Generic base class for test aggregates with typed state.
/// </summary>
/// <typeparam name="TState">The type of the aggregate state.</typeparam>
public abstract class TestAggregate<TState> : TestAggregate
    where TState : class, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestAggregate{TState}"/> class.
    /// </summary>
    protected TestAggregate()
    {
        State = new TState();
    }

    /// <summary>
    /// Gets the current state of the aggregate.
    /// </summary>
    protected TState State { get; private set; }

    /// <summary>
    /// Creates a snapshot of the current aggregate state.
    /// </summary>
    /// <returns>The snapshot representing the current state.</returns>
    protected override object CreateSnapshot()
    {
        return State;
    }

    /// <summary>
    /// Applies a snapshot to restore aggregate state.
    /// </summary>
    /// <param name="snapshot">The snapshot to apply.</param>
    protected override void ApplySnapshot(object snapshot)
    {
        State = (TState)snapshot;
    }

    /// <summary>
    /// Gets the current state of the aggregate.
    /// </summary>
    /// <returns>The current state.</returns>
    public TState GetState() => State;
}

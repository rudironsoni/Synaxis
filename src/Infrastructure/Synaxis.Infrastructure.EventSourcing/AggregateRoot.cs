// <copyright file="AggregateRoot.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using Synaxis.Abstractions.Cloud;
using Synaxis.Abstractions.Time;

/// <summary>
/// Base class for event-sourced aggregates that apply domain events to maintain state.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    /// <summary>
    /// The time provider for deterministic time access.
    /// </summary>
    private readonly ITimeProvider? _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot"/> class.
    /// Parameterless constructor for event reconstitution.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot"/> class with a time provider.
    /// </summary>
    /// <param name="timeProvider">The time provider for deterministic time access.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeProvider"/> is null.</exception>
    protected AggregateRoot(ITimeProvider timeProvider)
    {
        this._timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Gets the time provider for deterministic time access.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when time provider is not available.</exception>
    protected ITimeProvider TimeProvider =>
        this._timeProvider ?? throw new InvalidOperationException("TimeProvider not available. Use constructor with ITimeProvider for new aggregates.");

    /// <summary>
    /// Gets or sets the unique identifier of the aggregate.
    /// </summary>
    public string Id { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets the current version of the aggregate.
    /// </summary>
    public int Version { get; private set; } = 0;

    /// <summary>
    /// Gets the uncommitted events that have not yet been persisted.
    /// </summary>
    /// <returns>A read-only list of uncommitted events.</returns>
    public IReadOnlyList<IDomainEvent> GetUncommittedEvents()
    {
        return this._uncommittedEvents.AsReadOnly();
    }

    /// <summary>
    /// Marks all uncommitted events as committed.
    /// </summary>
    public void MarkAsCommitted()
    {
        this._uncommittedEvents.Clear();
    }

    /// <summary>
    /// Applies a domain event to the aggregate.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    protected void ApplyEvent(IDomainEvent @event)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        this.Apply(@event);
        this.Version++;
        this._uncommittedEvents.Add(@event);
    }

    /// <summary>
    /// Loads the aggregate state from a history of events.
    /// </summary>
    /// <param name="events">The events to load into the aggregate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="events"/> is null.</exception>
    public void LoadFromHistory(IEnumerable<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (var @event in events)
        {
            this.Apply(@event);
            this.Version++;
        }
    }

    /// <summary>
    /// Applies a domain event to update the aggregate state.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    protected abstract void Apply(IDomainEvent @event);
}

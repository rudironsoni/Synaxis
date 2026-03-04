// <copyright file="EventSourcedAggregate.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing.Aggregates;

using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Base class for aggregates that support event sourcing.
/// </summary>
public abstract class EventSourcedAggregate
{
    private readonly List<object> uncommittedEvents = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSourcedAggregate"/> class.
    /// </summary>
    protected EventSourcedAggregate()
    {
        this.Id = string.Empty;
    }

    /// <summary>
    /// Gets the unique identifier of the aggregate.
    /// </summary>
    public string Id { get; protected set; }

    /// <summary>
    /// Gets the current version of the aggregate.
    /// This represents the number of events that have been applied.
    /// </summary>
    public long Version { get; private set; } = -1;

    /// <summary>
    /// Gets or sets the original version of the aggregate before any new events.
    /// Used for optimistic concurrency checking.
    /// </summary>
    public long OriginalVersion => this.Version - this.uncommittedEvents.Count;

    /// <summary>
    /// Gets or sets the uncommitted events that have not yet been persisted.
    /// </summary>
    public IReadOnlyList<object> UncommittedEvents => this.uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Loads the aggregate from a history of events.
    /// </summary>
    /// <param name="events">The events to apply.</param>
    public void LoadFromHistory(IEnumerable<object> events)
    {
        if (events is null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        foreach (var @event in events)
        {
            this.ApplyEvent(@event, isNew: false);
        }
    }

    /// <summary>
    /// Raises a new event and applies it to the aggregate.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="event">The event to raise.</param>
    protected void RaiseEvent<TEvent>(TEvent @event)
        where TEvent : notnull
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        this.ApplyEvent(@event, isNew: true);
    }

    /// <summary>
    /// Applies an event to the aggregate state.
    /// </summary>
    /// <param name="event">The event to apply.</param>
    /// <param name="isNew">Whether this is a new event that should be tracked.</param>
    protected void ApplyEvent(object @event, bool isNew)
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        // Invoke the specific Apply method via reflection
        this.InvokeApplyMethod(@event);

        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
        else
        {
            this.Version++;
        }
    }

    /// <summary>
    /// Marks all uncommitted events as committed.
    /// </summary>
    public void MarkCommitted()
    {
        this.Version += uncommittedEvents.Count;
        uncommittedEvents.Clear();
    }

    /// <summary>
    /// Clears all uncommitted events without marking them as committed.
    /// </summary>
    public void ClearUncommittedEvents()
    {
        uncommittedEvents.Clear();
    }

    /// <summary>
    /// Invokes the Apply method for the specific event type.
    /// </summary>
    /// <param name="event">The event to apply.</param>
    private void InvokeApplyMethod(object @event)
    {
        var eventType = @event.GetType();
        var applyMethod = this.GetType().GetMethod(
            "Apply",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { eventType },
            null);

        if (applyMethod is null)
        {
            throw new InvalidOperationException(
                $"Aggregate {this.GetType().Name} does not have an Apply method for event type {eventType.Name}");
        }

        applyMethod.Invoke(this, new[] { @event });
    }
}

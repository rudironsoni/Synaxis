// <copyright file="DomainEvent.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Base implementation of <see cref="IDomainEvent"/> providing common event properties.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class.
    /// </summary>
    protected DomainEvent()
    {
        this.EventId = Guid.NewGuid().ToString();
        this.OccurredOn = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public string EventId { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the aggregate identifier for the event.
    /// </summary>
    public abstract string AggregateId { get; }

    /// <inheritdoc />
    public virtual string EventType => this.GetType().Name;
}

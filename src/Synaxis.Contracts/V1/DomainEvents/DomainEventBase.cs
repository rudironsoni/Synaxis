// <copyright file="DomainEventBase.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Base class for all domain events.
/// </summary>
public abstract record DomainEventBase
{
    /// <summary>
    /// Gets the unique identifier for the event.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("eventId")]
    public Guid EventId { get; init; }

    /// <summary>
    /// Gets the identifier of the aggregate that generated the event.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("aggregateId")]
    public Guid AggregateId { get; init; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the version of the event schema.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public int Version { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventBase"/> class.
    /// </summary>
    protected DomainEventBase()
    {
        this.EventId = Guid.NewGuid();
        this.Timestamp = DateTimeOffset.UtcNow;
    }
}

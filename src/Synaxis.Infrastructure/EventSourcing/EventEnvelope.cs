// <copyright file="EventEnvelope.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using System;

/// <summary>
/// Implementation of an event envelope containing metadata and event data.
/// </summary>
/// <typeparam name="TEvent">The type of the event.</typeparam>
public sealed class EventEnvelope<TEvent> : IEventEnvelope<TEvent>
    where TEvent : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventEnvelope{TEvent}"/> class.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="version">The version within the stream.</param>
    /// <param name="globalPosition">The global position across all streams.</param>
    /// <param name="eventData">The event data.</param>
    /// <param name="metadata">The event metadata.</param>
    /// <param name="timestamp">The timestamp when the event was recorded.</param>
    public EventEnvelope(
        Guid eventId,
        string streamId,
        long version,
        long globalPosition,
        TEvent eventData,
        EventMetadata? metadata = null,
        DateTime? timestamp = null)
    {
        this.EventId = eventId;
        this.StreamId = streamId;
        this.Version = version;
        this.GlobalPosition = globalPosition;
        this.EventData = eventData;
        this.Metadata = metadata ?? new EventMetadata();
        this.Timestamp = timestamp ?? DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public Guid EventId { get; }

    /// <inheritdoc/>
    public string StreamId { get; }

    /// <inheritdoc/>
    public long Version { get; }

    /// <inheritdoc/>
    public long GlobalPosition { get; }

    /// <inheritdoc/>
    public string EventType => typeof(TEvent).FullName ?? typeof(TEvent).Name;

    /// <inheritdoc/>
    public DateTime Timestamp { get; }

    /// <inheritdoc/>
    public TEvent EventData { get; }

    /// <inheritdoc/>
    object IEventEnvelope.EventData => this.EventData;

    /// <inheritdoc/>
    public EventMetadata Metadata { get; }
}

/// <summary>
/// Factory for creating untyped event envelopes from deserialized data.
/// </summary>
public sealed class EventEnvelope : IEventEnvelope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventEnvelope"/> class.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="version">The version within the stream.</param>
    /// <param name="globalPosition">The global position across all streams.</param>
    /// <param name="eventType">The type name of the event.</param>
    /// <param name="eventData">The event data.</param>
    /// <param name="metadata">The event metadata.</param>
    /// <param name="timestamp">The timestamp when the event was recorded.</param>
    public EventEnvelope(
        Guid eventId,
        string streamId,
        long version,
        long globalPosition,
        string eventType,
        object eventData,
        EventMetadata metadata,
        DateTime timestamp)
    {
        this.EventId = eventId;
        this.StreamId = streamId;
        this.Version = version;
        this.GlobalPosition = globalPosition;
        this.EventType = eventType;
        this.EventData = eventData;
        this.Metadata = metadata;
        this.Timestamp = timestamp;
    }

    /// <inheritdoc/>
    public Guid EventId { get; }

    /// <inheritdoc/>
    public string StreamId { get; }

    /// <inheritdoc/>
    public long Version { get; }

    /// <inheritdoc/>
    public long GlobalPosition { get; }

    /// <inheritdoc/>
    public string EventType { get; }

    /// <inheritdoc/>
    public DateTime Timestamp { get; }

    /// <inheritdoc/>
    public object EventData { get; }

    /// <inheritdoc/>
    public EventMetadata Metadata { get; }
}

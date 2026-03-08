// <copyright file="IEventEnvelope.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing;

using System;

/// <summary>
/// Represents an event envelope containing metadata and the event data.
/// </summary>
public interface IEventEnvelope
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the identifier of the stream this event belongs to.
    /// </summary>
    string StreamId { get; }

    /// <summary>
    /// Gets the version of the event within its stream.
    /// </summary>
    long Version { get; }

    /// <summary>
    /// Gets the global position of the event across all streams.
    /// </summary>
    long GlobalPosition { get; }

    /// <summary>
    /// Gets the type name of the event.
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the timestamp when the event was recorded.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Gets the event data.
    /// </summary>
    object EventData { get; }

    /// <summary>
    /// Gets the metadata associated with the event.
    /// </summary>
    EventMetadata Metadata { get; }
}

/// <summary>
/// Generic interface for typed event envelopes.
/// </summary>
/// <typeparam name="TEvent">The type of the event.</typeparam>
public interface IEventEnvelope<TEvent> : IEventEnvelope
    where TEvent : notnull
{
    /// <summary>
    /// Gets the strongly-typed event data.
    /// </summary>
    new TEvent EventData { get; }
}

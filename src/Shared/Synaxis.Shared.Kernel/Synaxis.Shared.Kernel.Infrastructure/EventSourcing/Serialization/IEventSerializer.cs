// <copyright file="IEventSerializer.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing.Serialization;

using System;
using System.Text.Json;

/// <summary>
/// Interface for serializing and deserializing events.
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serializes an event to JSON string.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="event">The event to serialize.</param>
    /// <returns>The JSON representation of the event.</returns>
    string Serialize<TEvent>(TEvent @event)
        where TEvent : notnull;

    /// <summary>
    /// Deserializes a JSON string to an event of the specified type.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <param name="eventType">The type of the event to deserialize.</param>
    /// <returns>The deserialized event object.</returns>
    object Deserialize(string json, Type eventType);

    /// <summary>
    /// Gets the event type from a type name, supporting polymorphic event handling.
    /// </summary>
    /// <param name="typeName">The full type name.</param>
    /// <returns>The resolved type, or null if not found.</returns>
    Type? ResolveType(string typeName);
}

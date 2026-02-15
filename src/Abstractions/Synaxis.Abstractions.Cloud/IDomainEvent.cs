// <copyright file="IDomainEvent.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

/// <summary>
/// Base interface for domain events in the system.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for the event.
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the type name of the event.
    /// </summary>
    string EventType { get; }
}

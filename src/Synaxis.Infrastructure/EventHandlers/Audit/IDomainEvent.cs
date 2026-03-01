// <copyright file="IDomainEvent.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventHandlers.Audit;

using System;

/// <summary>
/// Marker interface for domain events.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// Gets when this event occurred.
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the type of this event.
    /// </summary>
    string EventType { get; }
}

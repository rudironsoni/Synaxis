// <copyright file="EventMetadata.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using System;
using System.Collections.Generic;

/// <summary>
/// Contains metadata associated with an event.
/// </summary>
public sealed class EventMetadata
{
    /// <summary>
    /// Gets the correlation identifier used to track related operations across the system.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the identifier of the causative operation.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who triggered the event.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the timestamp when the event was originally triggered by the user/system.
    /// </summary>
    public DateTime? TriggeredAt { get; init; }

    /// <summary>
    /// Gets the name or identifier of the service that recorded the event.
    /// </summary>
    public string? ServiceName { get; init; }

    /// <summary>
    /// Gets the IP address of the client that triggered the event.
    /// </summary>
    public string? ClientIpAddress { get; init; }

    /// <summary>
    /// Gets the additional custom metadata properties.
    /// </summary>
    public IDictionary<string, string> CustomProperties { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new metadata instance with the specified correlation and causation IDs.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="causationId">The causation ID.</param>
    /// <returns>A new EventMetadata instance.</returns>
    public static EventMetadata Create(string? correlationId = null, string? causationId = null)
    {
        return new EventMetadata
        {
            CorrelationId = correlationId,
            CausationId = causationId,
        };
    }

    /// <summary>
    /// Creates a child metadata instance with the current event ID as the causation ID.
    /// </summary>
    /// <param name="eventId">The event ID to use as causation.</param>
    /// <returns>A new EventMetadata instance.</returns>
    public EventMetadata CreateChild(string eventId)
    {
        return new EventMetadata
        {
            CorrelationId = this.CorrelationId,
            CausationId = eventId,
            UserId = this.UserId,
            ServiceName = this.ServiceName,
            ClientIpAddress = this.ClientIpAddress,
        };
    }
}

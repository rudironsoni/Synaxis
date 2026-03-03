// <copyright file="UsageRecorded.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Aggregates.UsageTracking.Events;

using MediatR;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Domain event raised when usage is recorded.
/// </summary>
public record UsageRecorded : IDomainEvent, INotification
{
    /// <summary>
    /// Gets the unique identifier for the event.
    /// </summary>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the type name of the event.
    /// </summary>
    public string EventType => nameof(UsageRecorded);

    /// <summary>
    /// Gets the usage tracking aggregate identifier.
    /// </summary>
    public required Guid UsageTrackingId { get; init; }

    /// <summary>
    /// Gets the record identifier.
    /// </summary>
    public required Guid RecordId { get; init; }

    /// <summary>
    /// Gets the organization identifier.
    /// </summary>
    public required Guid OrganizationId { get; init; }

    /// <summary>
    /// Gets the resource type.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// Gets the resource identifier, if applicable.
    /// </summary>
    public required string? ResourceId { get; init; }

    /// <summary>
    /// Gets the quantity used.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the unit of measurement.
    /// </summary>
    public required string Unit { get; init; }

    /// <summary>
    /// Gets the timestamp when usage occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; init; }
}

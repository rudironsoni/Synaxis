// <copyright file="UsageTrackingCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Aggregates.UsageTracking.Events;

using MediatR;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Domain event raised when a new usage tracking aggregate is created.
/// </summary>
public record UsageTrackingCreated : IDomainEvent, INotification
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
    public string EventType => nameof(UsageTrackingCreated);

    /// <summary>
    /// Gets the usage tracking aggregate identifier.
    /// </summary>
    public required Guid UsageTrackingId { get; init; }

    /// <summary>
    /// Gets the organization identifier.
    /// </summary>
    public required Guid OrganizationId { get; init; }

    /// <summary>
    /// Gets the resource type being tracked.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// Gets the billing period.
    /// </summary>
    public required string BillingPeriod { get; init; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}

// <copyright file="UsageRecord.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Aggregates.UsageTracking;

using Billing.Domain.Aggregates.UsageTracking.ValueObjects;

/// <summary>
/// Represents a single usage record within the usage tracking aggregate.
/// </summary>
public class UsageRecord
{
    /// <summary>
    /// Gets the unique identifier for the usage record.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the organization identifier that owns this record.
    /// </summary>
    public Guid OrganizationId { get; }

    /// <summary>
    /// Gets the resource type being tracked.
    /// </summary>
    public string ResourceType { get; }

    /// <summary>
    /// Gets the resource identifier, if applicable.
    /// </summary>
    public string? ResourceId { get; }

    /// <summary>
    /// Gets the quantity of usage.
    /// </summary>
    public Quantity Quantity { get; }

    /// <summary>
    /// Gets the timestamp when the usage occurred.
    /// </summary>
    public UsageTimestamp Timestamp { get; }

    /// <summary>
    /// Gets additional metadata for the usage record.
    /// </summary>
    public UsageMetadata? Metadata { get; }

    /// <summary>
    /// Gets the timestamp when the record was created.
    /// </summary>
    public DateTime RecordedAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageRecord"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The optional resource identifier.</param>
    /// <param name="quantity">The quantity of usage.</param>
    /// <param name="timestamp">The usage timestamp.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <exception cref="ArgumentException">Thrown when resource type is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public UsageRecord(
        Guid id,
        Guid organizationId,
        string resourceType,
        string? resourceId,
        Quantity quantity,
        UsageTimestamp timestamp,
        UsageMetadata? metadata)
    {
        ArgumentException.ThrowIfNullOrEmpty(resourceType);

        Id = id;
        OrganizationId = organizationId;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Quantity = quantity ?? throw new ArgumentNullException(nameof(quantity));
        Timestamp = timestamp ?? throw new ArgumentNullException(nameof(timestamp));
        Metadata = metadata;
        RecordedAt = DateTime.UtcNow;
    }
}

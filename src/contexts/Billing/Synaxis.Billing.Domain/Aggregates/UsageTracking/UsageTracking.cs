// <copyright file="UsageTracking.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Aggregates.UsageTracking;

using Billing.Domain.Aggregates.UsageTracking.Events;
using Billing.Domain.Aggregates.UsageTracking.ValueObjects;
using Synaxis.Abstractions.Cloud;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Aggregate root for tracking usage of resources within an organization.
/// </summary>
public class UsageTracking : AggregateRoot
{
    private readonly List<UsageRecord> _records = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageTracking"/> class.
    /// Private constructor for event reconstitution.
    /// </summary>
    private UsageTracking()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageTracking"/> class.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="resourceType">The type of resource being tracked.</param>
    /// <param name="billingPeriod">The billing period identifier.</param>
    /// <exception cref="ArgumentException">Thrown when required parameters are empty.</exception>
    public UsageTracking(Guid organizationId, string resourceType, string billingPeriod)
    {
        ArgumentException.ThrowIfNullOrEmpty(resourceType);
        ArgumentException.ThrowIfNullOrEmpty(billingPeriod);

        var id = Guid.NewGuid();

        ApplyEvent(new UsageTrackingCreated
        {
            UsageTrackingId = id,
            OrganizationId = organizationId,
            ResourceType = resourceType,
            BillingPeriod = billingPeriod,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gets the organization identifier.
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Gets the resource type being tracked.
    /// </summary>
    public string ResourceType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the billing period.
    /// </summary>
    public string BillingPeriod { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the collection of usage records.
    /// </summary>
    public IReadOnlyCollection<UsageRecord> Records => _records.AsReadOnly();

    /// <summary>
    /// Records a usage event.
    /// </summary>
    /// <param name="quantity">The quantity of usage.</param>
    /// <param name="timestamp">The timestamp of the usage event.</param>
    /// <param name="resourceId">Optional resource identifier.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>The created usage record.</returns>
    /// <exception cref="InvalidUsageOperationException">Thrown when quantity is not positive.</exception>
    public UsageRecord RecordUsage(
        Quantity quantity,
        UsageTimestamp timestamp,
        ResourceIdentifier? resourceId = null,
        UsageMetadata? metadata = null)
    {
        if (quantity.Value <= 0)
            throw new InvalidUsageOperationException("Usage quantity must be positive");

        var recordId = Guid.NewGuid();

        var record = new UsageRecord(
            recordId,
            OrganizationId,
            ResourceType,
            resourceId?.Value,
            quantity,
            timestamp,
            metadata);

        ApplyEvent(new UsageRecorded
        {
            UsageTrackingId = Guid.Parse(Id),
            RecordId = recordId,
            OrganizationId = OrganizationId,
            ResourceType = ResourceType,
            ResourceId = resourceId?.Value,
            Quantity = quantity.Value,
            Unit = quantity.Unit,
            Timestamp = timestamp.Value,
            Metadata = metadata?.Data
        });

        return record;
    }

    /// <summary>
    /// Calculates the total usage across all records.
    /// </summary>
    /// <returns>The total quantity used.</returns>
    public decimal GetTotalUsage()
    {
        return _records.Sum(r => r.Quantity.Value);
    }

    /// <summary>
    /// Calculates the total cost based on unit price.
    /// </summary>
    /// <param name="unitPrice">The price per unit.</param>
    /// <returns>The total cost.</returns>
    public decimal GetTotalCost(decimal unitPrice)
    {
        return GetTotalUsage() * unitPrice;
    }

    /// <summary>
    /// Gets records within a specific time range.
    /// </summary>
    /// <param name="from">Start of the range.</param>
    /// <param name="to">End of the range.</param>
    /// <returns>Filtered and ordered usage records.</returns>
    public IReadOnlyList<UsageRecord> GetRecordsInRange(DateTime from, DateTime to)
    {
        return _records
            .Where(r => r.Timestamp.Value >= from && r.Timestamp.Value <= to)
            .OrderBy(r => r.Timestamp.Value)
            .ToList();
    }

    /// <summary>
    /// Applies a domain event to update the aggregate state.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case UsageTrackingCreated e:
                Id = e.UsageTrackingId.ToString();
                OrganizationId = e.OrganizationId;
                ResourceType = e.ResourceType;
                BillingPeriod = e.BillingPeriod;
                CreatedAt = e.CreatedAt;
                break;

            case UsageRecorded e:
                var record = new UsageRecord(
                    e.RecordId,
                    e.OrganizationId,
                    e.ResourceType,
                    e.ResourceId,
                    new Quantity(e.Quantity, e.Unit),
                    new UsageTimestamp(e.Timestamp),
                    e.Metadata != null ? new UsageMetadata(e.Metadata) : null);
                _records.Add(record);
                break;
        }
    }
}

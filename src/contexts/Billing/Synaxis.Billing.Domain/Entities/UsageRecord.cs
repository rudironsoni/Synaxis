// <copyright file="UsageRecord.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Entities;

/// <summary>
/// Represents a usage record for billing purposes.
/// </summary>
public class UsageRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for the usage record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the organization ID that owns this usage record.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the resource type (API, Compute, Storage, etc.).
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource ID, if applicable.
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the quantity used.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit of measurement (requests, hours, GB, etc.).
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the usage occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// <copyright file="DomainEventBase.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DomainEvents;

/// <summary>
/// Base class for all domain events (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - EventId renamed to Id for consistency
/// - Added TenantId for multi-tenancy support
/// - Added Source property for event sourcing
/// - Timestamp is now required in constructor.
/// </remarks>
public abstract record DomainEventBase
{
    /// <summary>
    /// Gets the unique identifier for the event.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the identifier of the aggregate that generated the event.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("aggregateId")]
    public Guid AggregateId { get; init; }

    /// <summary>
    /// Gets the identifier of the tenant (for multi-tenancy).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tenantId")]
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the version of the event schema.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public int Version { get; init; }

    /// <summary>
    /// Gets the source system or service that generated the event.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("source")]
    public string Source { get; init; } = "synaxis";

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventBase"/> class.
    /// </summary>
    protected DomainEventBase()
    {
        this.Id = Guid.NewGuid();
        this.Timestamp = DateTimeOffset.UtcNow;
    }
}

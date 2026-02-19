namespace Synaxis.Contracts.V2.DomainEvents;

/// <summary>
/// Base class for all domain events (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - EventId renamed to Id for consistency
/// - Added TenantId for multi-tenancy support
/// - Added Source property for event sourcing
/// - Timestamp is now required in constructor
/// </remarks>
public abstract record DomainEventBase
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>
    /// Identifier of the aggregate that generated the event.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("aggregateId")]
    public Guid AggregateId { get; init; }

    /// <summary>
    /// Identifier of the tenant (for multi-tenancy).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tenantId")]
    public string? TenantId { get; init; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Version of the event schema.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public int Version { get; init; }

    /// <summary>
    /// Source system or service that generated the event.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("source")]
    public string Source { get; init; } = "synaxis";

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventBase"/> class.
    /// </summary>
    protected DomainEventBase()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTimeOffset.UtcNow;
    }
}

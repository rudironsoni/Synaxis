namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Base class for all domain events.
/// </summary>
public abstract record DomainEventBase
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("eventId")]
    public Guid EventId { get; init; }

    /// <summary>
    /// Identifier of the aggregate that generated the event.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("aggregateId")]
    public Guid AggregateId { get; init; }

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
    /// Initializes a new instance of the <see cref="DomainEventBase"/> class.
    /// </summary>
    protected DomainEventBase()
    {
        EventId = Guid.NewGuid();
        Timestamp = DateTimeOffset.UtcNow;
    }
}

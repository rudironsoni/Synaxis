namespace Synaxis.Contracts.V1.Queries;

/// <summary>
/// Base class for all queries.
/// </summary>
public abstract record QueryBase
{
    /// <summary>
    /// Unique identifier for the query.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("queryId")]
    public Guid QueryId { get; init; }

    /// <summary>
    /// Correlation identifier for tracing requests across services.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Timestamp when the query was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Identifier of the user issuing the query.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("userId")]
    public required string UserId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBase"/> class.
    /// </summary>
    protected QueryBase()
    {
        QueryId = Guid.NewGuid();
        CorrelationId = Guid.NewGuid();
        Timestamp = DateTimeOffset.UtcNow;
    }
}

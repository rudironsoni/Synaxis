// <copyright file="QueryBase.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.Queries;

/// <summary>
/// Base class for all queries.
/// </summary>
public abstract record QueryBase
{
    /// <summary>
    /// Gets the unique identifier for the query.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("queryId")]
    public Guid QueryId { get; init; }

    /// <summary>
    /// Gets the correlation identifier for tracing requests across services.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the timestamp when the query was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the identifier of the user issuing the query.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("userId")]
    public required string UserId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBase"/> class.
    /// </summary>
    protected QueryBase()
    {
        this.QueryId = Guid.NewGuid();
        this.CorrelationId = Guid.NewGuid();
        this.Timestamp = DateTimeOffset.UtcNow;
    }
}

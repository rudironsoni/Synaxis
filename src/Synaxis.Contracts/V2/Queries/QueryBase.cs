// <copyright file="QueryBase.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.Queries;

/// <summary>
/// Base class for all queries (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - QueryId renamed to Id
/// - Added TenantId for multi-tenancy
/// - Added Source property.
/// </remarks>
public abstract record QueryBase
{
    /// <summary>
    /// Gets the unique identifier for the query.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the correlation identifier for tracing requests across services.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the identifier of the tenant (for multi-tenancy).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tenantId")]
    public string? TenantId { get; init; }

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
    /// Gets the source system or service that issued the query.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("source")]
    public string Source { get; init; } = "api";

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBase"/> class.
    /// </summary>
    protected QueryBase()
    {
        this.Id = Guid.NewGuid();
        this.CorrelationId = Guid.NewGuid();
        this.Timestamp = DateTimeOffset.UtcNow;
    }
}

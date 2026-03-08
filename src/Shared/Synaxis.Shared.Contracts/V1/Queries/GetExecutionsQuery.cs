// <copyright file="GetExecutionsQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.Queries;

/// <summary>
/// Query to get a paginated list of executions.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetExecutionsQuery), "get_executions")]
public record GetExecutionsQuery : QueryBase
{
    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("page")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("pageSize")]
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Gets the filter by agent identifier.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public Guid? AgentId { get; init; }

    /// <summary>
    /// Gets the filter by status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>
    /// Gets the filter by the user who initiated the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("initiatedByUserId")]
    public string? InitiatedByUserId { get; init; }

    /// <summary>
    /// Gets the filter for executions created after this date.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAfter")]
    public DateTimeOffset? CreatedAfter { get; init; }

    /// <summary>
    /// Gets the filter for executions created before this date.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdBefore")]
    public DateTimeOffset? CreatedBefore { get; init; }

    /// <summary>
    /// Gets the sort field.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("sortBy")]
    public string SortBy { get; init; } = "createdAt";

    /// <summary>
    /// Gets the sort direction (asc or desc).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("sortDirection")]
    public string SortDirection { get; init; } = "desc";
}

// <copyright file="GetUsersQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Queries;

/// <summary>
/// Query to get a paginated list of users.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetUsersQuery), "get_users")]
public record GetUsersQuery : QueryBase
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
    /// Gets the filter by status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>
    /// Gets the search term for name or email.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("search")]
    public string? Search { get; init; }

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

    /// <summary>
    /// Gets a value indicating whether to include soft-deleted users.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeDeleted")]
    public bool IncludeDeleted { get; init; }
}

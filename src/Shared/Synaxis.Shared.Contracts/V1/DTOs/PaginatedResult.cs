// <copyright file="PaginatedResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.DTOs;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">Type of items in the result.</typeparam>
public record PaginatedResult<T>
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("page")]
    public required int Page { get; init; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("pageSize")]
    public required int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("totalCount")]
    public required long TotalCount { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("totalPages")]
    public int TotalPages => (int)Math.Ceiling((double)this.TotalCount / this.PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => this.Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("hasNextPage")]
    public bool HasNextPage => this.Page < this.TotalPages;
}

// <copyright file="PaginatedResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">Type of items in the result.</typeparam>
public record PaginatedResult<T>
{
    /// <summary>
    /// Gets items in the current page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Gets current page number (1-based).
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Gets number of items per page.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Gets total number of items across all pages.
    /// </summary>
    public required long TotalCount { get; init; }

    /// <summary>
    /// Gets total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)this.TotalCount / this.PageSize);

    /// <summary>
    /// Gets a value indicating whether whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => this.Page > 1;

    /// <summary>
    /// Gets a value indicating whether whether there is a next page.
    /// </summary>
    public bool HasNextPage => this.Page < this.TotalPages;
}

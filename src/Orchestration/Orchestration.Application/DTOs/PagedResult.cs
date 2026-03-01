// <copyright file="PagedResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a paged result set.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items for the current page.</param>
/// <param name="TotalCount">The total item count.</param>
/// <param name="Page">The current page number.</param>
/// <param name="PageSize">The page size.</param>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)this.TotalCount / this.PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => this.Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => this.Page < this.TotalPages;
}

// <copyright file="PagedResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Represents a paginated result set.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    /// <param name="Items">The items on the current page.</param>
    /// <param name="TotalCount">The total count of items across all pages.</param>
    /// <param name="Page">The current page number (1-based).</param>
    /// <param name="PageSize">The number of items per page.</param>
    public record PagedResult<T>(
        IReadOnlyList<T> Items,
        int TotalCount,
        int Page,
        int PageSize)
    {
        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages => this.PageSize > 0 ? (int)Math.Ceiling((double)this.TotalCount / this.PageSize) : 0;

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => this.Page > 1;

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        public bool HasNextPage => this.Page < this.TotalPages;

        /// <summary>
        /// Creates an empty paged result.
        /// </summary>
        /// <returns>An empty paged result.</returns>
        public static PagedResult<T> Empty() => new([], 0, 1, 10);
    }
}

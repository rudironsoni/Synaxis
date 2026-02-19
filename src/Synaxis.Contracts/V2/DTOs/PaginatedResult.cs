namespace Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Represents a paginated result set (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added Cursor and PreviousCursor for cursor-based pagination
/// - Added HasPrevious flag
/// </remarks>
/// <typeparam name="T">Type of items in the result.</typeparam>
public record PaginatedResult<T>
{
    /// <summary>
    /// Items in the current page.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("page")]
    public required int Page { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("pageSize")]
    public required int PageSize { get; init; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("totalCount")]
    public required long TotalCount { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("totalPages")]
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("hasNextPage")]
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Cursor for the next page (cursor-based pagination).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cursor")]
    public string? Cursor { get; init; }

    /// <summary>
    /// Cursor for the previous page (cursor-based pagination).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("previousCursor")]
    public string? PreviousCursor { get; init; }
}

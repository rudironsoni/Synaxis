namespace Synaxis.Contracts.V2.Queries;

/// <summary>
/// Query to get a paginated list of users (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Cursor-based pagination added (Cursor, CursorDirection)
/// - Offset-based pagination still supported but deprecated
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetUsersQuery), "get_users")]
public record GetUsersQuery : QueryBase
{
    /// <summary>
    /// Page number (1-based).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("page")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("pageSize")]
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Cursor for cursor-based pagination (overrides page/pageSize).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cursor")]
    public string? Cursor { get; init; }

    /// <summary>
    /// Cursor direction (next or previous).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cursorDirection")]
    public string CursorDirection { get; init; } = "next";

    /// <summary>
    /// Filter by status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>
    /// Filter by admin status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isAdmin")]
    public bool? IsAdmin { get; init; }

    /// <summary>
    /// Search term for name or email.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("search")]
    public string? Search { get; init; }

    /// <summary>
    /// Sort field.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("sortBy")]
    public string SortBy { get; init; } = "createdAt";

    /// <summary>
    /// Sort direction (asc or desc).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("sortDirection")]
    public string SortDirection { get; init; } = "desc";

    /// <summary>
    /// Whether to include soft-deleted users.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeDeleted")]
    public bool IncludeDeleted { get; init; }

    /// <summary>
    /// Whether to include metadata.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeMetadata")]
    public bool IncludeMetadata { get; init; }
}

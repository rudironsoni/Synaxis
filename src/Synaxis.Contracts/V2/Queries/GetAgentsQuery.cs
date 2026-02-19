namespace Synaxis.Contracts.V2.Queries;

/// <summary>
/// Query to get a paginated list of agents (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Cursor-based pagination added
/// - Tags renamed to Labels
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetAgentsQuery), "get_agents")]
public record GetAgentsQuery : QueryBase
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
    /// Cursor for cursor-based pagination.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cursor")]
    public string? Cursor { get; init; }

    /// <summary>
    /// Cursor direction (next or previous).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cursorDirection")]
    public string CursorDirection { get; init; } = "next";

    /// <summary>
    /// Filter by agent type.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentType")]
    public string? AgentType { get; init; }

    /// <summary>
    /// Filter by status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>
    /// Search term for name or description.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("search")]
    public string? Search { get; init; }

    /// <summary>
    /// Filter by labels (key=value pairs).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; init; }

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
}

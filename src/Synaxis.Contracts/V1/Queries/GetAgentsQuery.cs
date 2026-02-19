namespace Synaxis.Contracts.V1.Queries;

/// <summary>
/// Query to get a paginated list of agents.
/// </summary>
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
    /// Filter by tags.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

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

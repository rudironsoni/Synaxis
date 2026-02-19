namespace Synaxis.Contracts.V1.Queries;

/// <summary>
/// Query to get a paginated list of executions.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetExecutionsQuery), "get_executions")]
public record GetExecutionsQuery : QueryBase
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
    /// Filter by agent identifier.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public Guid? AgentId { get; init; }

    /// <summary>
    /// Filter by status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>
    /// Filter by user who initiated the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("initiatedByUserId")]
    public string? InitiatedByUserId { get; init; }

    /// <summary>
    /// Filter executions created after this date.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAfter")]
    public DateTimeOffset? CreatedAfter { get; init; }

    /// <summary>
    /// Filter executions created before this date.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdBefore")]
    public DateTimeOffset? CreatedBefore { get; init; }

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

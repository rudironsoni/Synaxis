namespace Synaxis.Contracts.V2.Queries;

/// <summary>
/// Query to get an agent by its identifier (V2).
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetAgentByIdQuery), "get_agent_by_id")]
public record GetAgentByIdQuery : QueryBase
{
    /// <summary>
    /// Identifier of the agent to retrieve.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid TargetAgentId { get; init; }

    /// <summary>
    /// Whether to include configuration details.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeConfiguration")]
    public bool IncludeConfiguration { get; init; } = true;

    /// <summary>
    /// Whether to include resource requirements.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeResources")]
    public bool IncludeResources { get; init; } = true;

    /// <summary>
    /// Whether to include execution statistics.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeStats")]
    public bool IncludeStats { get; init; }
}

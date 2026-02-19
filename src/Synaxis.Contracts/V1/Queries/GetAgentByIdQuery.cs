namespace Synaxis.Contracts.V1.Queries;

/// <summary>
/// Query to get an agent by its identifier.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetAgentByIdQuery), "get_agent_by_id")]
public record GetAgentByIdQuery : QueryBase
{
    /// <summary>
    /// Identifier of the agent to retrieve.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("targetAgentId")]
    public required Guid TargetAgentId { get; init; }

    /// <summary>
    /// Whether to include configuration details.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeConfiguration")]
    public bool IncludeConfiguration { get; init; } = true;
}

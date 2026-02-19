namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Command to update an existing agent.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UpdateAgentCommand), "update_agent")]
public record UpdateAgentCommand : CommandBase
{
    /// <summary>
    /// Identifier of the agent to update.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid TargetAgentId { get; init; }

    /// <summary>
    /// Updated name (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Updated description (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Updated configuration (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("configuration")]
    public Dictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Updated tags (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Updated status (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }
}

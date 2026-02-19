namespace Synaxis.Contracts.V2.Commands;

/// <summary>
/// Command to update an existing agent (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Uses UpdateMask for partial updates
/// - Tags renamed to Labels
/// </remarks>
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
    /// Fields to update.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updateMask")]
    public required IReadOnlyList<string> UpdateMask { get; init; }

    /// <summary>
    /// Updated name.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Updated description.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Updated configuration.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("configuration")]
    public Dictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Updated resource requirements.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("resources")]
    public ResourceRequirements? Resources { get; init; }

    /// <summary>
    /// Updated labels.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; init; }

    /// <summary>
    /// Updated status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }
}

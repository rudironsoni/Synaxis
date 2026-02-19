using Synaxis.Contracts.V1.Common;

namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Event raised when a new agent is created.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AgentCreated), "agent_created")]
public record AgentCreated : DomainEventBase
{
    /// <summary>
    /// Name of the created agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Description of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Type of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentType")]
    public required string AgentType { get; init; }

    /// <summary>
    /// Configuration settings for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("configuration")]
    public Dictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Initial status of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public AgentStatus Status { get; init; } = AgentStatus.Creating;

    /// <summary>
    /// Identifier of the user who created the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdByUserId")]
    public required string CreatedByUserId { get; init; }

    /// <summary>
    /// Timestamp when the agent was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

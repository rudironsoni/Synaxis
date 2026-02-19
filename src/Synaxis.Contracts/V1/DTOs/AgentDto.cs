using Synaxis.Contracts.V1.Common;

namespace Synaxis.Contracts.V1.DTOs;

/// <summary>
/// Data transfer object for an agent.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AgentDto), "agent")]
public record AgentDto
{
    /// <summary>
    /// Unique identifier of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Name of the agent.
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
    /// Current status of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required AgentStatus Status { get; init; }

    /// <summary>
    /// Configuration settings for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("configuration")]
    public Dictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Tags for categorizing the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Identifier of the user who created the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdByUserId")]
    public required string CreatedByUserId { get; init; }

    /// <summary>
    /// Timestamp when the agent was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the agent was last updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Total number of executions for this agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionCount")]
    public int ExecutionCount { get; init; }
}

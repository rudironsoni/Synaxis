using Synaxis.Contracts.V2.Common;

namespace Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Data transfer object for an agent (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added ResourceRequirements
/// - Tags renamed to Labels
/// - Added ExecutionStats
/// </remarks>
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
    /// Resource requirements for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("resources")]
    public ResourceRequirements? Resources { get; init; }

    /// <summary>
    /// Labels for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; init; }

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
    /// Execution statistics.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stats")]
    public ExecutionStats? Stats { get; init; }
}

/// <summary>
/// Resource requirements for an agent.
/// </summary>
public record ResourceRequirements
{
    /// <summary>
    /// CPU limit in millicores.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cpu")]
    public string? Cpu { get; init; }

    /// <summary>
    /// Memory limit (e.g., "512Mi", "1Gi").
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("memory")]
    public string? Memory { get; init; }

    /// <summary>
    /// GPU requirements.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("gpu")]
    public string? Gpu { get; init; }
}

/// <summary>
/// Execution statistics for an agent.
/// </summary>
public record ExecutionStats
{
    /// <summary>
    /// Total number of executions.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    /// <summary>
    /// Number of successful executions.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("successCount")]
    public int SuccessCount { get; init; }

    /// <summary>
    /// Number of failed executions.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("failureCount")]
    public int FailureCount { get; init; }

    /// <summary>
    /// Average execution duration.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("averageDuration")]
    public TimeSpan? AverageDuration { get; init; }

    /// <summary>
    /// Last execution timestamp.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("lastExecutedAt")]
    public DateTimeOffset? LastExecutedAt { get; init; }
}

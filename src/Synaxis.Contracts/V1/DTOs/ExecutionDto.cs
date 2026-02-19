using Synaxis.Contracts.V1.Common;

namespace Synaxis.Contracts.V1.DTOs;

/// <summary>
/// Data transfer object for an agent execution.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ExecutionDto), "execution")]
public record ExecutionDto
{
    /// <summary>
    /// Unique identifier of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Identifier of the agent that was executed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Name of the agent that was executed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentName")]
    public required string AgentName { get; init; }

    /// <summary>
    /// Identifier of the user who initiated the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("initiatedByUserId")]
    public required string InitiatedByUserId { get; init; }

    /// <summary>
    /// Current status of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required ExecutionStatus Status { get; init; }

    /// <summary>
    /// Input parameters for the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("input")]
    public Dictionary<string, object>? Input { get; init; }

    /// <summary>
    /// Output from the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("output")]
    public Dictionary<string, object>? Output { get; init; }

    /// <summary>
    /// Error details if execution failed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Timestamp when the execution started.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Timestamp when the execution completed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Total duration of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("duration")]
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Number of tokens consumed (if applicable).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tokensConsumed")]
    public int? TokensConsumed { get; init; }

    /// <summary>
    /// Execution priority.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public int Priority { get; init; }
}

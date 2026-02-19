using Synaxis.Contracts.V1.Common;

namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Event raised when an agent execution starts.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AgentExecutionStarted), "agent_execution_started")]
public record AgentExecutionStarted : DomainEventBase
{
    /// <summary>
    /// Identifier of the agent being executed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Identifier of the user who initiated the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("initiatedByUserId")]
    public required string InitiatedByUserId { get; init; }

    /// <summary>
    /// Input parameters for the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("input")]
    public Dictionary<string, object>? Input { get; init; }

    /// <summary>
    /// Execution priority (higher = more urgent).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Maximum allowed execution time.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timeout")]
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Timestamp when the execution started.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Initial status of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public ExecutionStatus Status { get; init; } = ExecutionStatus.InProgress;
}

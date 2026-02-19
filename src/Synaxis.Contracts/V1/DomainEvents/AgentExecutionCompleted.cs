using Synaxis.Contracts.V1.Common;

namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Event raised when an agent execution completes.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AgentExecutionCompleted), "agent_execution_completed")]
public record AgentExecutionCompleted : DomainEventBase
{
    /// <summary>
    /// Identifier of the executed agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Final status of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required ExecutionStatus Status { get; init; }

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
    /// Timestamp when the execution completed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("completedAt")]
    public DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Total duration of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("duration")]
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of tokens consumed (if applicable).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tokensConsumed")]
    public int? TokensConsumed { get; init; }
}

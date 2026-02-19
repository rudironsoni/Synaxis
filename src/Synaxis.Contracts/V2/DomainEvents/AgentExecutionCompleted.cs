using Synaxis.Contracts.V2.Common;

namespace Synaxis.Contracts.V2.DomainEvents;

/// <summary>
/// Event raised when an agent execution completes (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added Metrics for performance tracking
/// - Output is now strongly typed
/// - Added ExecutionStages for detailed progress
/// </remarks>
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
    public ExecutionError? Error { get; init; }

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
    /// Execution metrics.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("metrics")]
    public ExecutionMetrics? Metrics { get; init; }

    /// <summary>
    /// Execution stages for detailed progress.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stages")]
    public IReadOnlyList<ExecutionStage>? Stages { get; init; }
}

/// <summary>
/// Execution error details.
/// </summary>
public record ExecutionError
{
    /// <summary>
    /// Error code.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Error details.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("details")]
    public string? Details { get; init; }

    /// <summary>
    /// Whether the error is retryable.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("retryable")]
    public bool Retryable { get; init; }
}

/// <summary>
/// Execution metrics.
/// </summary>
public record ExecutionMetrics
{
    /// <summary>
    /// Number of tokens consumed (if applicable).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tokensConsumed")]
    public int? TokensConsumed { get; init; }

    /// <summary>
    /// CPU time used.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cpuTime")]
    public TimeSpan? CpuTime { get; init; }

    /// <summary>
    /// Memory used in bytes.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("memoryBytes")]
    public long? MemoryBytes { get; init; }

    /// <summary>
    /// Custom metrics.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("custom")]
    public Dictionary<string, double>? Custom { get; init; }
}

/// <summary>
/// Execution stage details.
/// </summary>
public record ExecutionStage
{
    /// <summary>
    /// Name of the stage.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Status of the stage.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required ExecutionStatus Status { get; init; }

    /// <summary>
    /// Duration of the stage.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("duration")]
    public TimeSpan Duration { get; init; }
}

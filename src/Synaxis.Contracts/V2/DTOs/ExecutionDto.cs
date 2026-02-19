using Synaxis.Contracts.V2.Common;

namespace Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Data transfer object for an agent execution (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added ParentExecutionId for nested executions
/// - Added WorkflowId for workflow integration
/// - Added ExecutionContext for distributed tracing
/// - Added Metrics and Stages
/// </remarks>
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
    /// Parent execution identifier (for nested executions).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("parentExecutionId")]
    public Guid? ParentExecutionId { get; init; }

    /// <summary>
    /// Workflow identifier (if part of a workflow).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("workflowId")]
    public Guid? WorkflowId { get; init; }

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
    /// Execution context for distributed tracing.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionContext")]
    public ExecutionContext? ExecutionContext { get; init; }

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
    public ExecutionError? Error { get; init; }

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
    /// Execution priority.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public int Priority { get; init; }

    /// <summary>
    /// Execution metrics.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("metrics")]
    public ExecutionMetrics? Metrics { get; init; }

    /// <summary>
    /// Execution stages.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stages")]
    public IReadOnlyList<ExecutionStage>? Stages { get; init; }
}

/// <summary>
/// Execution context for distributed tracing.
/// </summary>
public record ExecutionContext
{
    /// <summary>
    /// Trace identifier for distributed tracing.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>
    /// Span identifier for distributed tracing.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("spanId")]
    public string? SpanId { get; init; }
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

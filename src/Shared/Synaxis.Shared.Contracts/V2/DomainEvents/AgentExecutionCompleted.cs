// <copyright file="AgentExecutionCompleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DomainEvents;

using Synaxis.Shared.Contracts.V2.Common;

/// <summary>
/// Event raised when an agent execution completes (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added Metrics for performance tracking
/// - Output is now strongly typed
/// - Added ExecutionStages for detailed progress.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AgentExecutionCompleted), "agent_execution_completed")]
public record AgentExecutionCompleted : DomainEventBase
{
    /// <summary>
    /// Gets the identifier of the executed agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Gets the final status of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required ExecutionStatus Status { get; init; }

    /// <summary>
    /// Gets the output from the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("output")]
    public IReadOnlyDictionary<string, object>? Output { get; init; }

    /// <summary>
    /// Gets the error details if execution failed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public ExecutionError? Error { get; init; }

    /// <summary>
    /// Gets the timestamp when the execution completed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("completedAt")]
    public DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Gets the total duration of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("duration")]
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the execution metrics.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("metrics")]
    public ExecutionMetrics? Metrics { get; init; }

    /// <summary>
    /// Gets the execution stages for detailed progress.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stages")]
    public IReadOnlyList<ExecutionStage>? Stages { get; init; }
}

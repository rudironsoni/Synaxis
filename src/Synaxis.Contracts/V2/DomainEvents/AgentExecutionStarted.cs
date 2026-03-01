// <copyright file="AgentExecutionStarted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.DomainEvents;

using Synaxis.Contracts.V2.Common;

/// <summary>
/// Event raised when an agent execution starts (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added ExecutionContext for distributed tracing
/// - Input is now strongly typed
/// - Added ResourceAllocation for scheduling.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AgentExecutionStarted), "agent_execution_started")]
public record AgentExecutionStarted : DomainEventBase
{
    /// <summary>
    /// Gets the identifier of the agent being executed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who initiated the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("initiatedByUserId")]
    public required string InitiatedByUserId { get; init; }

    /// <summary>
    /// Gets the execution context for distributed tracing.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionContext")]
    public ExecutionContext? ExecutionContext { get; init; }

    /// <summary>
    /// Gets the input parameters for the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("input")]
    public IReadOnlyDictionary<string, object>? Input { get; init; }

    /// <summary>
    /// Gets the execution priority (higher = more urgent).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Gets the maximum allowed execution time.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timeout")]
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets the resource allocation for the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("resources")]
    public ResourceRequirements? Resources { get; init; }

    /// <summary>
    /// Gets the timestamp when the execution started.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets the initial status of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public ExecutionStatus Status { get; init; } = ExecutionStatus.Running;
}

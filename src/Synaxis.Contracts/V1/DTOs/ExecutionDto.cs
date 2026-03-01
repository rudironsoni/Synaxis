// <copyright file="ExecutionDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.DTOs;

using Synaxis.Contracts.V1.Common;

/// <summary>
/// Data transfer object for an agent execution.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ExecutionDto), "execution")]
public record ExecutionDto
{
    /// <summary>
    /// Gets the unique identifier of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the identifier of the agent that was executed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Gets the name of the agent that was executed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentName")]
    public required string AgentName { get; init; }

    /// <summary>
    /// Gets the identifier of the user who initiated the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("initiatedByUserId")]
    public required string InitiatedByUserId { get; init; }

    /// <summary>
    /// Gets the current status of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required ExecutionStatus Status { get; init; }

    /// <summary>
    /// Gets the input parameters for the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("input")]
    public IReadOnlyDictionary<string, object>? Input { get; init; }

    /// <summary>
    /// Gets the output from the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("output")]
    public IReadOnlyDictionary<string, object>? Output { get; init; }

    /// <summary>
    /// Gets the error details if execution failed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Gets the timestamp when the execution started.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the execution completed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Gets the total duration of the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("duration")]
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets the number of tokens consumed (if applicable).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tokensConsumed")]
    public int? TokensConsumed { get; init; }

    /// <summary>
    /// Gets the execution priority.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public int Priority { get; init; }
}

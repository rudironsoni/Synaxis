// <copyright file="AgentExecutionDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Data transfer object for an agent execution.
/// </summary>
public record AgentExecutionDto
{
    /// <summary>
    /// Gets unique identifier of the execution.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets identifier of the agent.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Gets execution identifier string.
    /// </summary>
    public required string ExecutionId { get; init; }

    /// <summary>
    /// Gets current status of the execution.
    /// </summary>
    public required AgentStatus Status { get; init; }

    /// <summary>
    /// Gets input parameters for the execution.
    /// </summary>
    public required IReadOnlyDictionary<string, object> InputParameters { get; init; }

    /// <summary>
    /// Gets current step number.
    /// </summary>
    public required int CurrentStep { get; init; }

    /// <summary>
    /// Gets timestamp when the execution started.
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Gets timestamp when the execution completed.
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Gets error message if the execution failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets duration of the execution in milliseconds.
    /// </summary>
    public long? DurationMs { get; init; }

    /// <summary>
    /// Gets execution steps.
    /// </summary>
    public IReadOnlyList<ExecutionStepDto> Steps { get; init; } = Array.Empty<ExecutionStepDto>();
}

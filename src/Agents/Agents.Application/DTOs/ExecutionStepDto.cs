// <copyright file="ExecutionStepDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Data transfer object for an execution step.
/// </summary>
public record ExecutionStepDto
{
    /// <summary>
    /// Gets step number.
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// Gets name of the step.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets status of the step.
    /// </summary>
    public required AgentStatus Status { get; init; }

    /// <summary>
    /// Gets timestamp when the step started.
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Gets timestamp when the step completed.
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Gets output from the step.
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// Gets error message if the step failed.
    /// </summary>
    public string? Error { get; init; }
}

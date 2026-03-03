// <copyright file="ExecutionStep.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Represents a step in an agent execution workflow.
/// </summary>
public record ExecutionStep
{
    /// <summary>
    /// Gets the step number in the execution sequence.
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// Gets the name of the execution step.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the status of the execution step.
    /// </summary>
    public required AgentStatus Status { get; init; }

    /// <summary>
    /// Gets the timestamp when the step started.
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the step completed.
    /// </summary>
    public DateTime? CompletedAt { get; init; }
}

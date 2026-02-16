// <copyright file="WorkflowStepCompleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a workflow step is completed.
/// </summary>
public record WorkflowStepCompleted : IDomainEvent
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(WorkflowStepCompleted);

    /// <summary>
    /// Gets the unique identifier of the workflow.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the step number that was completed.
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// Gets the name of the step.
    /// </summary>
    public required string StepName { get; init; }

    /// <summary>
    /// Gets the timestamp when the step completed.
    /// </summary>
    public required DateTime CompletedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}

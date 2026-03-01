// <copyright file="WorkflowStepCompleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.DomainEvents;

using Synaxis.Contracts.V1.Common;

/// <summary>
/// Event raised when a workflow step completes.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(WorkflowStepCompleted), "workflow_step_completed")]
public record WorkflowStepCompleted : DomainEventBase
{
    /// <summary>
    /// Gets the identifier of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("workflowId")]
    public required Guid WorkflowId { get; init; }

    /// <summary>
    /// Gets the identifier of the completed step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepId")]
    public required Guid StepId { get; init; }

    /// <summary>
    /// Gets the name of the completed step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepName")]
    public required string StepName { get; init; }

    /// <summary>
    /// Gets the result of the step execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("result")]
    public IReadOnlyDictionary<string, object>? Result { get; init; }

    /// <summary>
    /// Gets the status of the step completion.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required ExecutionStatus Status { get; init; }

    /// <summary>
    /// Gets the identifier of the next step (null if last step).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("nextStepId")]
    public Guid? NextStepId { get; init; }

    /// <summary>
    /// Gets the timestamp when the step completed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("completedAt")]
    public DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Gets the duration of step execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("duration")]
    public TimeSpan Duration { get; init; }
}

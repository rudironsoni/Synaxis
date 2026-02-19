using Synaxis.Contracts.V2.Common;

namespace Synaxis.Contracts.V2.DomainEvents;

/// <summary>
/// Event raised when a workflow step completes (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added Metrics for step performance
/// - Added Attempts for retry tracking
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(WorkflowStepCompleted), "workflow_step_completed")]
public record WorkflowStepCompleted : DomainEventBase
{
    /// <summary>
    /// Identifier of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("workflowId")]
    public required Guid WorkflowId { get; init; }

    /// <summary>
    /// Execution identifier.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionId")]
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Identifier of the completed step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepId")]
    public required Guid StepId { get; init; }

    /// <summary>
    /// Name of the completed step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepName")]
    public required string StepName { get; init; }

    /// <summary>
    /// Result of the step execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("result")]
    public Dictionary<string, object>? Result { get; init; }

    /// <summary>
    /// Status of the step completion.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required ExecutionStatus Status { get; init; }

    /// <summary>
    /// Identifier of the next step (null if last step).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("nextStepId")]
    public Guid? NextStepId { get; init; }

    /// <summary>
    /// Timestamp when the step completed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("completedAt")]
    public DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Duration of step execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("duration")]
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of attempts made (including retries).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("attempts")]
    public int Attempts { get; init; } = 1;

    /// <summary>
    /// Step metrics.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("metrics")]
    public Dictionary<string, double>? Metrics { get; init; }
}

using Synaxis.Contracts.V2.Common;

namespace Synaxis.Contracts.V2.DomainEvents;

/// <summary>
/// Event raised when a workflow fails (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added ExecutionId for traceability
/// - Error is now strongly typed
/// - Added FailedSteps for multiple failures
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(WorkflowFailed), "workflow_failed")]
public record WorkflowFailed : DomainEventBase
{
    /// <summary>
    /// Identifier of the failed workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("workflowId")]
    public required Guid WorkflowId { get; init; }

    /// <summary>
    /// Execution identifier.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionId")]
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Primary error that caused the failure.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public required ExecutionError Error { get; init; }

    /// <summary>
    /// Steps that failed (for parallel execution scenarios).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("failedSteps")]
    public IReadOnlyList<FailedStepInfo>? FailedSteps { get; init; }

    /// <summary>
    /// Whether the workflow can be retried.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("retryable")]
    public bool Retryable { get; init; }

    /// <summary>
    /// Number of retry attempts made.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("retryCount")]
    public int RetryCount { get; init; }

    /// <summary>
    /// Timestamp when the workflow failed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("failedAt")]
    public DateTimeOffset FailedAt { get; init; }
}

/// <summary>
/// Information about a failed step.
/// </summary>
public record FailedStepInfo
{
    /// <summary>
    /// Identifier of the failed step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepId")]
    public required Guid StepId { get; init; }

    /// <summary>
    /// Name of the failed step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepName")]
    public required string StepName { get; init; }

    /// <summary>
    /// Error details.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public required ExecutionError Error { get; init; }
}

using Synaxis.Contracts.V1.Common;

namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Event raised when a workflow fails.
/// </summary>
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
    /// Identifier of the step that failed (if applicable).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("failedStepId")]
    public Guid? FailedStepId { get; init; }

    /// <summary>
    /// Name of the step that failed (if applicable).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("failedStepName")]
    public string? FailedStepName { get; init; }

    /// <summary>
    /// Error message describing the failure.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("errorMessage")]
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Error code for the failure.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Stack trace if available.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stackTrace")]
    public string? StackTrace { get; init; }

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

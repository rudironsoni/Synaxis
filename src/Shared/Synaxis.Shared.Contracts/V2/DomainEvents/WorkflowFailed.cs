// <copyright file="WorkflowFailed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DomainEvents;

using Synaxis.Shared.Contracts.V2.Common;

/// <summary>
/// Event raised when a workflow fails (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added ExecutionId for traceability
/// - Error is now strongly typed
/// - Added FailedSteps for multiple failures.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(WorkflowFailed), "workflow_failed")]
public record WorkflowFailed : DomainEventBase
{
    /// <summary>
    /// Gets the identifier of the failed workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("workflowId")]
    public required Guid WorkflowId { get; init; }

    /// <summary>
    /// Gets the execution identifier.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionId")]
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Gets the primary error that caused the failure.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public required ExecutionError Error { get; init; }

    /// <summary>
    /// Gets the steps that failed (for parallel execution scenarios).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("failedSteps")]
    public IReadOnlyList<FailedStepInfo>? FailedSteps { get; init; }

    /// <summary>
    /// Gets a value indicating whether the workflow can be retried.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("retryable")]
    public bool Retryable { get; init; }

    /// <summary>
    /// Gets the number of retry attempts made.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("retryCount")]
    public int RetryCount { get; init; }

    /// <summary>
    /// Gets the timestamp when the workflow failed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("failedAt")]
    public DateTimeOffset FailedAt { get; init; }
}

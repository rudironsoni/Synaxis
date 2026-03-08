// <copyright file="WorkflowFailed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.DomainEvents;

using Synaxis.Shared.Contracts.V1.Common;

/// <summary>
/// Event raised when a workflow fails.
/// </summary>
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
    /// Gets the identifier of the step that failed (if applicable).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("failedStepId")]
    public Guid? FailedStepId { get; init; }

    /// <summary>
    /// Gets the name of the step that failed (if applicable).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("failedStepName")]
    public string? FailedStepName { get; init; }

    /// <summary>
    /// Gets the error message describing the failure.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("errorMessage")]
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the error code for the failure.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets the stack trace if available.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stackTrace")]
    public string? StackTrace { get; init; }

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

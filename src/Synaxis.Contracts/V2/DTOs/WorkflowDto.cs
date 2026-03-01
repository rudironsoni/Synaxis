// <copyright file="WorkflowDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.DTOs;

using Synaxis.Contracts.V2.Common;

/// <summary>
/// Data transfer object for a workflow (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added Schedule for recurring workflows
/// - Added Variables for workflow variables
/// - Steps now include RetryPolicy.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(WorkflowDto), "workflow")]
public record WorkflowDto
{
    /// <summary>
    /// Gets the unique identifier of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the name of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the current status of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required WorkflowStatus Status { get; init; }

    /// <summary>
    /// Gets the steps in the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("steps")]
    public IReadOnlyList<WorkflowStepDto> Steps { get; init; } = Array.Empty<WorkflowStepDto>();

    /// <summary>
    /// Gets the workflow variables.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("variables")]
    public IReadOnlyDictionary<string, WorkflowVariable>? Variables { get; init; }

    /// <summary>
    /// Gets the schedule for recurring workflows.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("schedule")]
    public WorkflowSchedule? Schedule { get; init; }

    /// <summary>
    /// Gets the identifier of the user who created the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdByUserId")]
    public required string CreatedByUserId { get; init; }

    /// <summary>
    /// Gets the timestamp when the workflow was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the workflow was last updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the workflow was started.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("startedAt")]
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the workflow was completed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Gets the total number of executions for this workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionCount")]
    public int ExecutionCount { get; init; }
}

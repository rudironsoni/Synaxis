using Synaxis.Contracts.V1.Common;

namespace Synaxis.Contracts.V1.DTOs;

/// <summary>
/// Data transfer object for a workflow.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(WorkflowDto), "workflow")]
public record WorkflowDto
{
    /// <summary>
    /// Unique identifier of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Name of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Description of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Current status of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required WorkflowStatus Status { get; init; }

    /// <summary>
    /// Steps in the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("steps")]
    public IReadOnlyList<WorkflowStepDto> Steps { get; init; } = Array.Empty<WorkflowStepDto>();

    /// <summary>
    /// Identifier of the user who created the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdByUserId")]
    public required string CreatedByUserId { get; init; }

    /// <summary>
    /// Timestamp when the workflow was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the workflow was last updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Timestamp when the workflow was started.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("startedAt")]
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Timestamp when the workflow was completed.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Total number of executions for this workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionCount")]
    public int ExecutionCount { get; init; }
}

/// <summary>
/// Data transfer object for a workflow step.
/// </summary>
public record WorkflowStepDto
{
    /// <summary>
    /// Unique identifier for the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Name of the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Type of the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepType")]
    public required string StepType { get; init; }

    /// <summary>
    /// Configuration for the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("configuration")]
    public Dictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Order of execution for this step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("order")]
    public int Order { get; init; }

    /// <summary>
    /// Current status of the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public ExecutionStatus Status { get; init; } = ExecutionStatus.Pending;
}

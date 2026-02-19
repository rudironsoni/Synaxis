using Synaxis.Contracts.V1.Common;

namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Event raised when a new workflow is created.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(WorkflowCreated), "workflow_created")]
public record WorkflowCreated : DomainEventBase
{
    /// <summary>
    /// Name of the created workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Description of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Steps in the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("steps")]
    public IReadOnlyList<WorkflowStepDefinition> Steps { get; init; } = Array.Empty<WorkflowStepDefinition>();

    /// <summary>
    /// Initial status of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public WorkflowStatus Status { get; init; } = WorkflowStatus.Pending;

    /// <summary>
    /// Identifier of the user who created the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdByUserId")]
    public required string CreatedByUserId { get; init; }

    /// <summary>
    /// Timestamp when the workflow was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Defines a step within a workflow.
/// </summary>
public record WorkflowStepDefinition
{
    /// <summary>
    /// Unique identifier for the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepId")]
    public required Guid StepId { get; init; }

    /// <summary>
    /// Name of the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Type of the step (e.g., agent, condition, delay).
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
}

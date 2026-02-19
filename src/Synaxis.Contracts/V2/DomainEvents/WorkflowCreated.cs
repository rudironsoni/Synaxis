using Synaxis.Contracts.V2.Common;

namespace Synaxis.Contracts.V2.DomainEvents;

/// <summary>
/// Event raised when a new workflow is created (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Steps now use strongly typed definitions
/// - Added Schedule for scheduled workflows
/// - Added Variables for workflow variables
/// </remarks>
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
    /// Workflow variables.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("variables")]
    public Dictionary<string, WorkflowVariable>? Variables { get; init; }

    /// <summary>
    /// Schedule for recurring workflows.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("schedule")]
    public WorkflowSchedule? Schedule { get; init; }

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
/// Defines a step within a workflow (V2).
/// </summary>
public record WorkflowStepDefinition
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
    /// Condition for executing this step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("condition")]
    public string? Condition { get; init; }

    /// <summary>
    /// Retry policy for the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("retryPolicy")]
    public RetryPolicy? RetryPolicy { get; init; }
}

/// <summary>
/// Workflow variable definition.
/// </summary>
public record WorkflowVariable
{
    /// <summary>
    /// Variable name.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Variable type.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Default value.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("defaultValue")]
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Whether the variable is required.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("required")]
    public bool Required { get; init; }
}

/// <summary>
/// Workflow schedule definition.
/// </summary>
public record WorkflowSchedule
{
    /// <summary>
    /// Cron expression for scheduling.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cron")]
    public string? Cron { get; init; }

    /// <summary>
    /// Time zone for the schedule.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timezone")]
    public string Timezone { get; init; } = "UTC";

    /// <summary>
    /// Whether the schedule is enabled.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;
}

/// <summary>
/// Retry policy for workflow steps.
/// </summary>
public record RetryPolicy
{
    /// <summary>
    /// Maximum number of retries.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("maxRetries")]
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Initial delay between retries.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("initialDelay")]
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Backoff multiplier for retries.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("backoffMultiplier")]
    public double BackoffMultiplier { get; init; } = 2.0;
}

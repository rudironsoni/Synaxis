// <copyright file="WorkflowCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.DomainEvents;

using Synaxis.Contracts.V2.Common;

/// <summary>
/// Event raised when a new workflow is created (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Steps now use strongly typed definitions
/// - Added Schedule for scheduled workflows
/// - Added Variables for workflow variables.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(WorkflowCreated), "workflow_created")]
public record WorkflowCreated : DomainEventBase
{
    /// <summary>
    /// Gets the name of the created workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the steps in the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("steps")]
    public IReadOnlyList<WorkflowStepDefinition> Steps { get; init; } = Array.Empty<WorkflowStepDefinition>();

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
    /// Gets the initial status of the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public WorkflowStatus Status { get; init; } = WorkflowStatus.Pending;

    /// <summary>
    /// Gets the identifier of the user who created the workflow.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdByUserId")]
    public required string CreatedByUserId { get; init; }

    /// <summary>
    /// Gets the timestamp when the workflow was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

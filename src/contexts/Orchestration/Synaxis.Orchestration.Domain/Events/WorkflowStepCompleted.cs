// <copyright file="WorkflowStepCompleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a workflow step completes.
/// </summary>
public class WorkflowStepCompleted : DomainEvent
{
    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the completed step index.
    /// </summary>
    public int StepIndex { get; set; }

    /// <summary>
    /// Gets or sets the step output data.
    /// </summary>
    public string? StepOutput { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.WorkflowId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(WorkflowStepCompleted);
}

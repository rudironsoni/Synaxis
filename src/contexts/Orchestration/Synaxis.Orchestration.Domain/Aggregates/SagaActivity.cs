// <copyright file="SagaActivity.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Represents an activity within a saga.
/// </summary>
public class SagaActivity
{
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the activity name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequence order.
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Gets or sets the workflow definition identifier.
    /// </summary>
    public Guid WorkflowDefinitionId { get; set; }

    /// <summary>
    /// Gets or sets the compensation activity identifier.
    /// </summary>
    public Guid? CompensationActivityId { get; set; }

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public ActivityStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the executed workflow identifier.
    /// </summary>
    public Guid? WorkflowId { get; set; }
}

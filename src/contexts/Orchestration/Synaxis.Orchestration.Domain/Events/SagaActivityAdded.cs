// <copyright file="SagaActivityAdded.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an activity is added to a saga.
/// </summary>
public class SagaActivityAdded : DomainEvent
{
    /// <summary>
    /// Gets or sets the saga identifier.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public Guid ActivityId { get; set; }

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
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.SagaId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(SagaActivityAdded);
}

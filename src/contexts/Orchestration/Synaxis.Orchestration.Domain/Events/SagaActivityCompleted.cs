// <copyright file="SagaActivityCompleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a saga activity completes.
/// </summary>
public class SagaActivityCompleted : DomainEvent
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
    /// Gets or sets the workflow identifier that was executed.
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.SagaId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(SagaActivityCompleted);
}

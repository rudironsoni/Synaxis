// <copyright file="WorkflowStarted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using MediatR;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a workflow starts execution.
/// </summary>
public class WorkflowStarted : DomainEvent, INotification
{
    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.WorkflowId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(WorkflowStarted);
}

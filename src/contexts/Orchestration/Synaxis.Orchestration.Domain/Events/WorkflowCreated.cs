// <copyright file="WorkflowCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using MediatR;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a new orchestration workflow is created.
/// </summary>
public class WorkflowCreated : DomainEvent, INotification
{
    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the workflow name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the workflow definition identifier.
    /// </summary>
    public Guid WorkflowDefinitionId { get; set; }

    /// <summary>
    /// Gets or sets the parent saga identifier if applicable.
    /// </summary>
    public Guid? SagaId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the total number of steps.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Gets or sets the input data.
    /// </summary>
    public string? InputData { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.WorkflowId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(WorkflowCreated);
}

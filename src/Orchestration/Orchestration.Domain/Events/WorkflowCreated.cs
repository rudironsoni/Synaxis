// <copyright file="WorkflowCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a new orchestration workflow is created.
/// </summary>
public class WorkflowCreated : DomainEvent
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

/// <summary>
/// Event raised when a workflow starts execution.
/// </summary>
public class WorkflowStarted : DomainEvent
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

/// <summary>
/// Event raised when a workflow completes successfully.
/// </summary>
public class WorkflowCompleted : DomainEvent
{
    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the output data.
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.WorkflowId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(WorkflowCompleted);
}

/// <summary>
/// Event raised when a workflow fails.
/// </summary>
public class WorkflowFailed : DomainEvent
{
    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.WorkflowId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(WorkflowFailed);
}

/// <summary>
/// Event raised when a workflow is compensated.
/// </summary>
public class WorkflowCompensated : DomainEvent
{
    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the compensation reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.WorkflowId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(WorkflowCompensated);
}

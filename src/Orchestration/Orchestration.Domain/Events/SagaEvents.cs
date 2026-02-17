// <copyright file="SagaEvents.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a new saga is created.
/// </summary>
public class SagaCreated : DomainEvent
{
    /// <summary>
    /// Gets or sets the saga identifier.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the saga name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the saga description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.SagaId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(SagaCreated);
}

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

/// <summary>
/// Event raised when a saga starts execution.
/// </summary>
public class SagaStarted : DomainEvent
{
    /// <summary>
    /// Gets or sets the saga identifier.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.SagaId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(SagaStarted);
}

/// <summary>
/// Event raised when a saga activity starts.
/// </summary>
public class SagaActivityStarted : DomainEvent
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
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.SagaId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(SagaActivityStarted);
}

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

/// <summary>
/// Event raised when a saga completes successfully.
/// </summary>
public class SagaCompleted : DomainEvent
{
    /// <summary>
    /// Gets or sets the saga identifier.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.SagaId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(SagaCompleted);
}

/// <summary>
/// Event raised when a saga fails.
/// </summary>
public class SagaFailed : DomainEvent
{
    /// <summary>
    /// Gets or sets the saga identifier.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the failed activity identifier.
    /// </summary>
    public Guid FailedActivityId { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.SagaId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(SagaFailed);
}

/// <summary>
/// Event raised when a saga activity is compensated.
/// </summary>
public class SagaActivityCompensated : DomainEvent
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
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.SagaId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(SagaActivityCompensated);
}

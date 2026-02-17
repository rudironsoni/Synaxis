// <copyright file="ActivityEvents.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a new activity is created.
/// </summary>
public class ActivityCreated : DomainEvent
{
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public Guid ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the activity name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the activity type.
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent workflow identifier if applicable.
    /// </summary>
    public Guid? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the parent saga identifier if applicable.
    /// </summary>
    public Guid? SagaId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the input data.
    /// </summary>
    public string? InputData { get; set; }

    /// <summary>
    /// Gets or sets the execution context.
    /// </summary>
    public string? ExecutionContext { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ActivityId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ActivityCreated);
}

/// <summary>
/// Event raised when an activity starts execution.
/// </summary>
public class ActivityStarted : DomainEvent
{
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public Guid ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ActivityId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ActivityStarted);
}

/// <summary>
/// Event raised when an activity completes successfully.
/// </summary>
public class ActivityCompleted : DomainEvent
{
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public Guid ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the output data.
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ActivityId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ActivityCompleted);
}

/// <summary>
/// Event raised when an activity fails.
/// </summary>
public class ActivityFailed : DomainEvent
{
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public Guid ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ActivityId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ActivityFailed);
}

/// <summary>
/// Event raised when an activity is retried.
/// </summary>
public class ActivityRetried : DomainEvent
{
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public Guid ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the retry count.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ActivityId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ActivityRetried);
}

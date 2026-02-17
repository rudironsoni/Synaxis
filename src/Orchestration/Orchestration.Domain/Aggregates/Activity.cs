// <copyright file="Activity.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Aggregates;

using Synaxis.Infrastructure.EventSourcing;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Aggregate root representing a standalone activity execution.
/// Activities are the smallest unit of work in orchestration.
/// </summary>
public class Activity : AggregateRoot
{
    /// <summary>
    /// Gets the activity identifier.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets the activity name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the activity type.
    /// </summary>
    public string ActivityType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the parent workflow identifier if applicable.
    /// </summary>
    public Guid? WorkflowId { get; private set; }

    /// <summary>
    /// Gets the parent saga identifier if applicable.
    /// </summary>
    public Guid? SagaId { get; private set; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the current status.
    /// </summary>
    public ActivityExecutionStatus Status { get; private set; }

    /// <summary>
    /// Gets the input data.
    /// </summary>
    public string? InputData { get; private set; }

    /// <summary>
    /// Gets the output data.
    /// </summary>
    public string? OutputData { get; private set; }

    /// <summary>
    /// Gets the execution context (e.g., agent ID, provider, etc.).
    /// </summary>
    public string? ExecutionContext { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the started timestamp.
    /// </summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>
    /// Gets the completed timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the error message if the activity failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Creates a new activity.
    /// </summary>
    public static Activity Create(
        Guid id,
        string name,
        string activityType,
        Guid? workflowId,
        Guid? sagaId,
        Guid tenantId,
        string? inputData,
        string? executionContext)
    {
        var activity = new Activity();
        var @event = new ActivityCreated
        {
            ActivityId = id,
            Name = name,
            ActivityType = activityType,
            WorkflowId = workflowId,
            SagaId = sagaId,
            TenantId = tenantId,
            InputData = inputData,
            ExecutionContext = executionContext,
            Timestamp = DateTime.UtcNow,
        };

        activity.ApplyEvent(@event);
        return activity;
    }

    /// <summary>
    /// Starts the activity execution.
    /// </summary>
    public void Start()
    {
        if (this.Status != ActivityExecutionStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start activity in {this.Status} status.");
        }

        var @event = new ActivityStarted
        {
            ActivityId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Completes the activity successfully.
    /// </summary>
    public void Complete(string outputData)
    {
        if (this.Status != ActivityExecutionStatus.Running)
        {
            throw new InvalidOperationException($"Cannot complete activity in {this.Status} status.");
        }

        var @event = new ActivityCompleted
        {
            ActivityId = this.Id,
            OutputData = outputData,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Fails the activity.
    /// </summary>
    public void Fail(string errorMessage)
    {
        if (this.Status != ActivityExecutionStatus.Running && this.Status != ActivityExecutionStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot fail activity in {this.Status} status.");
        }

        var @event = new ActivityFailed
        {
            ActivityId = this.Id,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Retries the activity after a failure.
    /// </summary>
    public void Retry(int retryCount)
    {
        var @event = new ActivityRetried
        {
            ActivityId = this.Id,
            RetryCount = retryCount,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ActivityCreated created:
                this.ApplyCreated(created);
                break;
            case ActivityStarted:
                this.ApplyStarted();
                break;
            case ActivityCompleted completed:
                this.ApplyCompleted(completed);
                break;
            case ActivityFailed failed:
                this.ApplyFailed(failed);
                break;
            case ActivityRetried:
                this.ApplyRetried();
                break;
        }
    }

    private void ApplyCreated(ActivityCreated @event)
    {
        this.Id = @event.ActivityId;
        this.Name = @event.Name;
        this.ActivityType = @event.ActivityType;
        this.WorkflowId = @event.WorkflowId;
        this.SagaId = @event.SagaId;
        this.TenantId = @event.TenantId;
        this.InputData = @event.InputData;
        this.ExecutionContext = @event.ExecutionContext;
        this.Status = ActivityExecutionStatus.Pending;
        this.CreatedAt = @event.Timestamp;
    }

    private void ApplyStarted()
    {
        this.Status = ActivityExecutionStatus.Running;
        this.StartedAt = DateTime.UtcNow;
    }

    private void ApplyCompleted(ActivityCompleted @event)
    {
        this.Status = ActivityExecutionStatus.Completed;
        this.OutputData = @event.OutputData;
        this.CompletedAt = DateTime.UtcNow;
    }

    private void ApplyFailed(ActivityFailed @event)
    {
        this.Status = ActivityExecutionStatus.Failed;
        this.ErrorMessage = @event.ErrorMessage;
        this.CompletedAt = DateTime.UtcNow;
    }

    private void ApplyRetried()
    {
        this.Status = ActivityExecutionStatus.Pending;
        this.StartedAt = null;
        this.CompletedAt = null;
        this.ErrorMessage = null;
    }
}

/// <summary>
/// Represents the status of an activity execution.
/// </summary>
public enum ActivityExecutionStatus
{
    /// <summary>
    /// Activity is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Activity is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Activity completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Activity failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Activity was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Activity is waiting for external input.
    /// </summary>
    Waiting,
}

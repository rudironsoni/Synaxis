// <copyright file="Saga.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Aggregates;

using Synaxis.Infrastructure.EventSourcing;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Aggregate root representing a Saga - a long-running business process
/// that coordinates multiple workflows with compensation support.
/// </summary>
public class Saga : AggregateRoot
{
    private readonly List<SagaActivity> _activities = new();

    /// <summary>
    /// Gets the saga identifier.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets the saga name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the saga description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the current status.
    /// </summary>
    public SagaStatus Status { get; private set; }

    /// <summary>
    /// Gets the activities in this saga.
    /// </summary>
    public IReadOnlyList<SagaActivity> Activities => this._activities.AsReadOnly();

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
    /// Gets the error message if the saga failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Creates a new saga.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The name of the saga.</param>
    /// <param name="description">The description of the saga.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>A new saga instance.</returns>
    public static Saga Create(
        Guid id,
        string name,
        string? description,
        Guid tenantId)
    {
        var saga = new Saga();
        var @event = new SagaCreated
        {
            SagaId = id,
            Name = name,
            Description = description,
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow,
        };

        saga.ApplyEvent(@event);
        return saga;
    }

    /// <summary>
    /// Adds an activity to the saga.
    /// </summary>
    /// <param name="activityId">The unique activity identifier.</param>
    /// <param name="name">The name of the activity.</param>
    /// <param name="sequence">The sequence number of the activity.</param>
    /// <param name="workflowDefinitionId">The workflow definition identifier.</param>
    /// <param name="compensationActivityId">The compensation activity identifier if applicable.</param>
    public void AddActivity(
        Guid activityId,
        string name,
        int sequence,
        Guid workflowDefinitionId,
        Guid? compensationActivityId)
    {
        var @event = new SagaActivityAdded
        {
            SagaId = this.Id,
            ActivityId = activityId,
            Name = name,
            Sequence = sequence,
            WorkflowDefinitionId = workflowDefinitionId,
            CompensationActivityId = compensationActivityId,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Starts the saga execution.
    /// </summary>
    public void Start()
    {
        if (this.Status != SagaStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start saga in {this.Status} status.");
        }

        var @event = new SagaStarted
        {
            SagaId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Marks an activity as started.
    /// </summary>
    /// <param name="activityId">The unique identifier of the activity to start.</param>
    public void StartActivity(Guid activityId)
    {
        var activity = this._activities.FirstOrDefault(a => a.Id == activityId);
        if (activity == null)
        {
            throw new InvalidOperationException($"Activity {activityId} not found in saga.");
        }

        var @event = new SagaActivityStarted
        {
            SagaId = this.Id,
            ActivityId = activityId,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Marks an activity as completed.
    /// </summary>
    /// <param name="activityId">The activity identifier.</param>
    /// <param name="workflowId">The workflow identifier.</param>
    public void CompleteActivity(Guid activityId, Guid workflowId)
    {
        var activity = this._activities.FirstOrDefault(a => a.Id == activityId);
        if (activity == null)
        {
            throw new InvalidOperationException($"Activity {activityId} not found in saga.");
        }

        var @event = new SagaActivityCompleted
        {
            SagaId = this.Id,
            ActivityId = activityId,
            WorkflowId = workflowId,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Completes the saga successfully.
    /// </summary>
    public void Complete()
    {
        if (this.Status != SagaStatus.Running)
        {
            throw new InvalidOperationException($"Cannot complete saga in {this.Status} status.");
        }

        var @event = new SagaCompleted
        {
            SagaId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Fails the saga and triggers compensation.
    /// </summary>
    /// <param name="failedActivityId">The identifier of the failed activity.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    public void Fail(Guid failedActivityId, string errorMessage)
    {
        if (this.Status != SagaStatus.Running)
        {
            throw new InvalidOperationException($"Cannot fail saga in {this.Status} status.");
        }

        var @event = new SagaFailed
        {
            SagaId = this.Id,
            FailedActivityId = failedActivityId,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Marks an activity as compensated.
    /// </summary>
    /// <param name="activityId">The activity identifier.</param>
    public void CompensateActivity(Guid activityId)
    {
        var @event = new SagaActivityCompensated
        {
            SagaId = this.Id,
            ActivityId = activityId,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case SagaCreated created:
                this.ApplyCreated(created);
                break;
            case SagaActivityAdded activityAdded:
                this.ApplyActivityAdded(activityAdded);
                break;
            case SagaStarted:
                this.ApplyStarted();
                break;
            case SagaActivityStarted:
                this.ApplyActivityStarted();
                break;
            case SagaActivityCompleted activityCompleted:
                this.ApplyActivityCompleted(activityCompleted);
                break;
            case SagaCompleted:
                this.ApplyCompleted();
                break;
            case SagaFailed failed:
                this.ApplyFailed(failed);
                break;
            case SagaActivityCompensated compensated:
                this.ApplyActivityCompensated(compensated);
                break;
        }
    }

    private void ApplyCreated(SagaCreated @event)
    {
        this.Id = @event.SagaId;
        this.Name = @event.Name;
        this.Description = @event.Description;
        this.TenantId = @event.TenantId;
        this.Status = SagaStatus.Pending;
        this.CreatedAt = @event.Timestamp;
    }

    private void ApplyActivityAdded(SagaActivityAdded @event)
    {
        this._activities.Add(new SagaActivity
        {
            Id = @event.ActivityId,
            Name = @event.Name,
            Sequence = @event.Sequence,
            WorkflowDefinitionId = @event.WorkflowDefinitionId,
            CompensationActivityId = @event.CompensationActivityId,
            Status = ActivityStatus.Pending,
        });
    }

    private void ApplyStarted()
    {
        this.Status = SagaStatus.Running;
        this.StartedAt = DateTime.UtcNow;
    }

    private void ApplyActivityStarted()
    {
        // Activity start is tracked externally; no state change needed in saga aggregate.
    }

    private void ApplyActivityCompleted(SagaActivityCompleted @event)
    {
        var activity = this._activities.FirstOrDefault(a => a.Id == @event.ActivityId);
        if (activity != null)
        {
            activity.Status = ActivityStatus.Completed;
            activity.WorkflowId = @event.WorkflowId;
        }
    }

    private void ApplyCompleted()
    {
        this.Status = SagaStatus.Completed;
        this.CompletedAt = DateTime.UtcNow;
    }

    private void ApplyFailed(SagaFailed @event)
    {
        this.Status = SagaStatus.Failed;
        this.ErrorMessage = @event.ErrorMessage;
        this.CompletedAt = DateTime.UtcNow;

        var activity = this._activities.FirstOrDefault(a => a.Id == @event.FailedActivityId);
        if (activity != null)
        {
            activity.Status = ActivityStatus.Failed;
        }
    }

    private void ApplyActivityCompensated(SagaActivityCompensated @event)
    {
        var activity = this._activities.FirstOrDefault(a => a.Id == @event.ActivityId);
        if (activity != null)
        {
            activity.Status = ActivityStatus.Compensated;
        }
    }
}

// <copyright file="OrchestrationWorkflow.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Aggregates;

using Synaxis.Infrastructure.EventSourcing;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Aggregate root representing an orchestration workflow.
/// Manages the lifecycle of a multi-step business process.
/// </summary>
public class OrchestrationWorkflow : AggregateRoot
{
    /// <summary>
    /// Gets the workflow identifier.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets the workflow name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the workflow description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the workflow definition identifier.
    /// </summary>
    public Guid WorkflowDefinitionId { get; private set; }

    /// <summary>
    /// Gets the parent saga identifier if this workflow is part of a saga.
    /// </summary>
    public Guid? SagaId { get; private set; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the current status.
    /// </summary>
    public WorkflowStatus Status { get; private set; }

    /// <summary>
    /// Gets the current step index.
    /// </summary>
    public int CurrentStepIndex { get; private set; }

    /// <summary>
    /// Gets the total step count.
    /// </summary>
    public int TotalSteps { get; private set; }

    /// <summary>
    /// Gets the input data.
    /// </summary>
    public string? InputData { get; private set; }

    /// <summary>
    /// Gets the output data.
    /// </summary>
    public string? OutputData { get; private set; }

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
    /// Gets the error message if the workflow failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Creates a new orchestration workflow.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The name of the workflow.</param>
    /// <param name="description">The description of the workflow.</param>
    /// <param name="workflowDefinitionId">The workflow definition identifier.</param>
    /// <param name="sagaId">The parent saga identifier if applicable.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="totalSteps">The total number of steps.</param>
    /// <param name="inputData">The input data.</param>
    /// <returns>A new orchestration workflow instance.</returns>
    public static OrchestrationWorkflow Create(
        Guid id,
        string name,
        string? description,
        Guid workflowDefinitionId,
        Guid? sagaId,
        Guid tenantId,
        int totalSteps,
        string? inputData)
    {
        var workflow = new OrchestrationWorkflow();
        var @event = new WorkflowCreated
        {
            WorkflowId = id,
            Name = name,
            Description = description,
            WorkflowDefinitionId = workflowDefinitionId,
            SagaId = sagaId,
            TenantId = tenantId,
            TotalSteps = totalSteps,
            InputData = inputData,
            Timestamp = DateTime.UtcNow,
        };

        workflow.ApplyEvent(@event);
        return workflow;
    }

    /// <summary>
    /// Starts the workflow execution.
    /// </summary>
    public void Start()
    {
        if (this.Status != WorkflowStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start workflow in {this.Status} status.");
        }

        var @event = new WorkflowStarted
        {
            WorkflowId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Progresses the workflow to the next step.
    /// </summary>
    /// <param name="stepOutput">The output data from the completed step.</param>
    public void ProgressStep(string stepOutput)
    {
        if (this.Status != WorkflowStatus.Running)
        {
            throw new InvalidOperationException($"Cannot progress workflow in {this.Status} status.");
        }

        var @event = new WorkflowStepCompleted
        {
            WorkflowId = this.Id,
            StepIndex = this.CurrentStepIndex,
            StepOutput = stepOutput,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Completes the workflow successfully.
    /// </summary>
    /// <param name="outputData">The final output data from the workflow.</param>
    public void Complete(string outputData)
    {
        if (this.Status != WorkflowStatus.Running)
        {
            throw new InvalidOperationException($"Cannot complete workflow in {this.Status} status.");
        }

        var @event = new WorkflowCompleted
        {
            WorkflowId = this.Id,
            OutputData = outputData,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Fails the workflow.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    public void Fail(string errorMessage)
    {
        if (this.Status != WorkflowStatus.Running && this.Status != WorkflowStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot fail workflow in {this.Status} status.");
        }

        var @event = new WorkflowFailed
        {
            WorkflowId = this.Id,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Compensates the workflow due to a failure in a parent saga.
    /// </summary>
    /// <param name="reason">The reason for the compensation.</param>
    public void Compensate(string reason)
    {
        var @event = new WorkflowCompensated
        {
            WorkflowId = this.Id,
            Reason = reason,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case WorkflowCreated created:
                this.ApplyCreated(created);
                break;
            case WorkflowStarted:
                this.ApplyStarted();
                break;
            case WorkflowStepCompleted stepCompleted:
                this.ApplyStepCompleted(stepCompleted);
                break;
            case WorkflowCompleted completed:
                this.ApplyCompleted(completed);
                break;
            case WorkflowFailed failed:
                this.ApplyFailed(failed);
                break;
            case WorkflowCompensated:
                this.ApplyCompensated();
                break;
        }
    }

    private void ApplyCreated(WorkflowCreated @event)
    {
        this.Id = @event.WorkflowId;
        this.Name = @event.Name;
        this.Description = @event.Description;
        this.WorkflowDefinitionId = @event.WorkflowDefinitionId;
        this.SagaId = @event.SagaId;
        this.TenantId = @event.TenantId;
        this.TotalSteps = @event.TotalSteps;
        this.InputData = @event.InputData;
        this.Status = WorkflowStatus.Pending;
        this.CurrentStepIndex = 0;
        this.CreatedAt = @event.Timestamp;
    }

    private void ApplyStarted()
    {
        this.Status = WorkflowStatus.Running;
        this.StartedAt = DateTime.UtcNow;
    }

    private void ApplyStepCompleted(WorkflowStepCompleted @event)
    {
        this.CurrentStepIndex = @event.StepIndex + 1;
    }

    private void ApplyCompleted(WorkflowCompleted @event)
    {
        this.Status = WorkflowStatus.Completed;
        this.OutputData = @event.OutputData;
        this.CompletedAt = DateTime.UtcNow;
    }

    private void ApplyFailed(WorkflowFailed @event)
    {
        this.Status = WorkflowStatus.Failed;
        this.ErrorMessage = @event.ErrorMessage;
        this.CompletedAt = DateTime.UtcNow;
    }

    private void ApplyCompensated()
    {
        this.Status = WorkflowStatus.Compensated;
        this.CompletedAt = DateTime.UtcNow;
    }
}

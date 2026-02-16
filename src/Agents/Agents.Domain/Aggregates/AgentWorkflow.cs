// <copyright file="AgentWorkflow.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Aggregates;

using Synaxis.Abstractions.Cloud;
using Synaxis.Agents.Domain.Events;
using Synaxis.Agents.Domain.ValueObjects;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Aggregate root representing an agent workflow.
/// </summary>
public class AgentWorkflow : AggregateRoot
{
    private readonly List<int> _completedSteps = new();
    private string _name = string.Empty;
    private string? _description;
    private string _workflowYaml = string.Empty;
    private string _tenantId = string.Empty;
    private string? _teamId;
    private AgentStatus _status;
    private int _currentStep;
    private int _retryCount;

    /// <summary>
    /// Gets the name of the workflow.
    /// </summary>
    public string Name => this._name;

    /// <summary>
    /// Gets the description of the workflow.
    /// </summary>
    public string? Description => this._description;

    /// <summary>
    /// Gets the YAML configuration for the workflow.
    /// </summary>
    public string WorkflowYaml => this._workflowYaml;

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId => this._tenantId;

    /// <summary>
    /// Gets the team identifier.
    /// </summary>
    public string? TeamId => this._teamId;

    /// <summary>
    /// Gets the current status of the workflow.
    /// </summary>
    public AgentStatus Status => this._status;

    /// <summary>
    /// Gets the current step number.
    /// </summary>
    public int CurrentStep => this._currentStep;

    /// <summary>
    /// Gets the number of retry attempts.
    /// </summary>
    public int RetryCount => this._retryCount;

    /// <summary>
    /// Gets the list of completed step numbers.
    /// </summary>
    public IReadOnlyList<int> CompletedSteps => this._completedSteps.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentWorkflow"/> class.
    /// Required for deserialization.
    /// </summary>
    private AgentWorkflow()
    {
    }

    /// <summary>
    /// Creates a new agent workflow.
    /// </summary>
    /// <param name="id">The unique identifier of the workflow.</param>
    /// <param name="name">The name of the workflow.</param>
    /// <param name="description">The description of the workflow.</param>
    /// <param name="workflowYaml">The YAML configuration for the workflow.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="teamId">The team identifier.</param>
    /// <returns>A new instance of <see cref="AgentWorkflow"/>.</returns>
    public static AgentWorkflow Create(
        Guid id,
        string name,
        string? description,
        string workflowYaml,
        string tenantId,
        string? teamId)
    {
        var workflow = new AgentWorkflow();
        var @event = new WorkflowCreated
        {
            Id = id,
            Name = name,
            Description = description,
            WorkflowYaml = workflowYaml,
            TenantId = tenantId,
            TeamId = teamId,
            Version = 1,
        };
        workflow.ApplyEvent(@event);
        return workflow;
    }

    /// <summary>
    /// Marks a workflow step as completed.
    /// </summary>
    /// <param name="stepNumber">The step number.</param>
    /// <param name="stepName">The name of the step.</param>
    public void CompleteStep(int stepNumber, string stepName)
    {
        if (this._status != AgentStatus.Running)
        {
            throw new InvalidOperationException($"Cannot complete step from status {this._status}.");
        }

        if (this._completedSteps.Contains(stepNumber))
        {
            throw new InvalidOperationException($"Step {stepNumber} is already completed.");
        }

        var @event = new WorkflowStepCompleted
        {
            Id = Guid.Parse(this.Id),
            StepNumber = stepNumber,
            StepName = stepName,
            CompletedAt = DateTime.UtcNow,
            Timestamp = DateTimeOffset.UtcNow,
        };
        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Marks the workflow as failed.
    /// </summary>
    /// <param name="stepNumber">The step number where the failure occurred.</param>
    /// <param name="error">The error message.</param>
    public void Fail(int stepNumber, string error)
    {
        if (this._status != AgentStatus.Running)
        {
            throw new InvalidOperationException($"Cannot fail workflow from status {this._status}.");
        }

        var @event = new WorkflowFailed
        {
            Id = Guid.Parse(this.Id),
            StepNumber = stepNumber,
            Error = error,
            FailedAt = DateTime.UtcNow,
            Timestamp = DateTimeOffset.UtcNow,
        };
        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Retries a failed workflow step.
    /// </summary>
    /// <param name="stepNumber">The step number to retry.</param>
    public void Retry(int stepNumber)
    {
        if (this._status != AgentStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot retry workflow from status {this._status}.");
        }

        this._retryCount++;

        var @event = new WorkflowRetried
        {
            Id = Guid.Parse(this.Id),
            StepNumber = stepNumber,
            RetryAttempt = this._retryCount,
            RetriedAt = DateTime.UtcNow,
        };
        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case WorkflowCreated created:
                this.Apply(created);
                break;
            case WorkflowStepCompleted completed:
                this.Apply(completed);
                break;
            case WorkflowFailed failed:
                this.Apply(failed);
                break;
            case WorkflowRetried retried:
                this.Apply(retried);
                break;
        }
    }

    private void Apply(WorkflowCreated @event)
    {
        this.Id = @event.Id.ToString();
        this._name = @event.Name;
        this._description = @event.Description;
        this._workflowYaml = @event.WorkflowYaml;
        this._tenantId = @event.TenantId;
        this._teamId = @event.TeamId;
        this._status = AgentStatus.Idle;
        this._currentStep = 0;
        this._retryCount = 0;
    }

    private void Apply(WorkflowStepCompleted @event)
    {
        this._completedSteps.Add(@event.StepNumber);
        this._currentStep = @event.StepNumber;
    }

    private void Apply(WorkflowFailed @event)
    {
        this._status = AgentStatus.Failed;
        this._currentStep = @event.StepNumber;
    }

    private void Apply(WorkflowRetried @event)
    {
        this._status = AgentStatus.Running;
        this._currentStep = @event.StepNumber;
    }
}

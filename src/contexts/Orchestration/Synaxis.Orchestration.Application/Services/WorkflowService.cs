// <copyright file="WorkflowService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Application.Services;

using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Implementation of workflow service.
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<WorkflowService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowService"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="logger">The logger.</param>
    public WorkflowService(IEventStore eventStore, ILogger<WorkflowService> logger)
    {
        this._eventStore = eventStore;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<OrchestrationWorkflow> CreateWorkflowAsync(
        string name,
        string? description,
        Guid workflowDefinitionId,
        Guid? sagaId,
        Guid tenantId,
        int totalSteps,
        string? inputData,
        CancellationToken cancellationToken = default)
    {
        var workflowId = Guid.NewGuid();
        var workflow = OrchestrationWorkflow.Create(
            workflowId,
            name,
            description,
            workflowDefinitionId,
            sagaId,
            tenantId,
            totalSteps,
            inputData);

        await this._eventStore.AppendAsync(
            workflowId.ToString(),
            workflow.GetUncommittedEvents(),
            0,
            cancellationToken).ConfigureAwait(false);

        workflow.MarkAsCommitted();

        this._logger.LogInformation(
            "Created workflow {WorkflowId} for tenant {TenantId}",
            workflowId,
            tenantId);

        return workflow;
    }

    /// <inheritdoc/>
    public async Task StartWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        var workflow = await this.LoadWorkflowAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found.");
        }

        workflow.Start();

        await this._eventStore.AppendAsync(
            workflowId.ToString(),
            workflow.GetUncommittedEvents(),
            workflow.Version - workflow.GetUncommittedEvents().Count,
            cancellationToken).ConfigureAwait(false);

        workflow.MarkAsCommitted();

        this._logger.LogInformation("Started workflow {WorkflowId}", workflowId);
    }

    /// <inheritdoc/>
    public async Task ProgressStepAsync(Guid workflowId, string stepOutput, CancellationToken cancellationToken = default)
    {
        var workflow = await this.LoadWorkflowAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found.");
        }

        workflow.ProgressStep(stepOutput);

        await this._eventStore.AppendAsync(
            workflowId.ToString(),
            workflow.GetUncommittedEvents(),
            workflow.Version - workflow.GetUncommittedEvents().Count,
            cancellationToken).ConfigureAwait(false);

        workflow.MarkAsCommitted();

        this._logger.LogInformation(
            "Progressed workflow {WorkflowId} to step {StepIndex}/{TotalSteps}",
            workflowId,
            workflow.CurrentStepIndex,
            workflow.TotalSteps);
    }

    /// <inheritdoc/>
    public async Task CompleteWorkflowAsync(Guid workflowId, string outputData, CancellationToken cancellationToken = default)
    {
        var workflow = await this.LoadWorkflowAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found.");
        }

        workflow.Complete(outputData);

        await this._eventStore.AppendAsync(
            workflowId.ToString(),
            workflow.GetUncommittedEvents(),
            workflow.Version - workflow.GetUncommittedEvents().Count,
            cancellationToken).ConfigureAwait(false);

        workflow.MarkAsCommitted();

        this._logger.LogInformation("Completed workflow {WorkflowId}", workflowId);
    }

    /// <inheritdoc/>
    public async Task FailWorkflowAsync(Guid workflowId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var workflow = await this.LoadWorkflowAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found.");
        }

        workflow.Fail(errorMessage);

        await this._eventStore.AppendAsync(
            workflowId.ToString(),
            workflow.GetUncommittedEvents(),
            workflow.Version - workflow.GetUncommittedEvents().Count,
            cancellationToken).ConfigureAwait(false);

        workflow.MarkAsCommitted();

        this._logger.LogError("Failed workflow {WorkflowId}: {ErrorMessage}", workflowId, errorMessage);
    }

    /// <inheritdoc/>
    public async Task CompensateWorkflowAsync(Guid workflowId, string reason, CancellationToken cancellationToken = default)
    {
        var workflow = await this.LoadWorkflowAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found.");
        }

        workflow.Compensate(reason);

        await this._eventStore.AppendAsync(
            workflowId.ToString(),
            workflow.GetUncommittedEvents(),
            workflow.Version - workflow.GetUncommittedEvents().Count,
            cancellationToken).ConfigureAwait(false);

        workflow.MarkAsCommitted();

        this._logger.LogWarning("Compensated workflow {WorkflowId}: {Reason}", workflowId, reason);
    }

    /// <inheritdoc/>
    public Task<OrchestrationWorkflow?> GetWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return this.LoadWorkflowAsync(workflowId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<OrchestrationWorkflow>> GetWorkflowsByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // In production, this would query a read model or projection
        return Task.FromResult<IReadOnlyList<OrchestrationWorkflow>>(new List<OrchestrationWorkflow>());
    }

    private async Task<OrchestrationWorkflow?> LoadWorkflowAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        var events = await this._eventStore.ReadStreamAsync(workflowId.ToString(), cancellationToken).ConfigureAwait(false);
        if (events.Count == 0)
        {
            return null;
        }

        var workflow = new OrchestrationWorkflow();
        workflow.LoadFromHistory(events);
        return workflow;
    }
}

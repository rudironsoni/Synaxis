// <copyright file="IWorkflowService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Application.Services;

using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Service for managing workflow lifecycle.
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// Creates a new workflow.
    /// </summary>
    /// <param name="name">The name of the workflow.</param>
    /// <param name="description">The description of the workflow.</param>
    /// <param name="workflowDefinitionId">The workflow definition identifier.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="totalSteps">The total number of steps in the workflow.</param>
    /// <param name="inputData">The input data for the workflow.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created workflow.</returns>
    Task<OrchestrationWorkflow> CreateWorkflowAsync(
        string name,
        string? description,
        Guid workflowDefinitionId,
        Guid? sagaId,
        Guid tenantId,
        int totalSteps,
        string? inputData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Progresses a workflow step.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="stepOutput">The step output data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProgressStepAsync(Guid workflowId, string stepOutput, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="outputData">The output data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompleteWorkflowAsync(Guid workflowId, string outputData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fails a workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FailWorkflowAsync(Guid workflowId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensates a workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="reason">The reason for compensation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompensateWorkflowAsync(Guid workflowId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workflow by ID.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The workflow if found; otherwise, null.</returns>
    Task<OrchestrationWorkflow?> GetWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflows by tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of workflows for the tenant.</returns>
    Task<IReadOnlyList<OrchestrationWorkflow>> GetWorkflowsByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

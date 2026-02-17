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
    Task StartWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Progresses a workflow step.
    /// </summary>
    Task ProgressStepAsync(Guid workflowId, string stepOutput, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a workflow.
    /// </summary>
    Task CompleteWorkflowAsync(Guid workflowId, string outputData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fails a workflow.
    /// </summary>
    Task FailWorkflowAsync(Guid workflowId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensates a workflow.
    /// </summary>
    Task CompensateWorkflowAsync(Guid workflowId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workflow by ID.
    /// </summary>
    Task<OrchestrationWorkflow?> GetWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflows by tenant.
    /// </summary>
    Task<IReadOnlyList<OrchestrationWorkflow>> GetWorkflowsByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing saga lifecycle.
/// </summary>
public interface ISagaService
{
    /// <summary>
    /// Creates a new saga.
    /// </summary>
    Task<Saga> CreateSagaAsync(
        string name,
        string? description,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an activity to a saga.
    /// </summary>
    Task AddActivityAsync(
        Guid sagaId,
        string name,
        int sequence,
        Guid workflowDefinitionId,
        Guid? compensationActivityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a saga.
    /// </summary>
    Task StartSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an activity in a saga.
    /// </summary>
    Task CompleteActivityAsync(
        Guid sagaId,
        Guid activityId,
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a saga.
    /// </summary>
    Task CompleteSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fails a saga.
    /// </summary>
    Task FailSagaAsync(
        Guid sagaId,
        Guid failedActivityId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a saga by ID.
    /// </summary>
    Task<Saga?> GetSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas by tenant.
    /// </summary>
    Task<IReadOnlyList<Saga>> GetSagasByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing activity execution.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Creates a new activity.
    /// </summary>
    Task<Activity> CreateActivityAsync(
        string name,
        string activityType,
        Guid? workflowId,
        Guid? sagaId,
        Guid tenantId,
        string? inputData,
        string? executionContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an activity.
    /// </summary>
    Task StartActivityAsync(Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an activity.
    /// </summary>
    Task CompleteActivityAsync(Guid activityId, string outputData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fails an activity.
    /// </summary>
    Task FailActivityAsync(Guid activityId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries an activity.
    /// </summary>
    Task RetryActivityAsync(Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an activity by ID.
    /// </summary>
    Task<Activity?> GetActivityAsync(Guid activityId, CancellationToken cancellationToken = default);
}

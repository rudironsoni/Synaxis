// <copyright file="IWorkflowRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure.Repositories;

using Synaxis.Orchestration.Domain;
using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Repository for workflow aggregates.
/// </summary>
public interface IWorkflowRepository
{
    /// <summary>
    /// Gets a workflow by ID.
    /// </summary>
    /// <param name="id">The workflow identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The workflow if found; otherwise, <see langword="null"/>.</returns>
    Task<OrchestrationWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a workflow.
    /// </summary>
    /// <param name="workflow">The workflow to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAsync(OrchestrationWorkflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflows by tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of workflows for the specified tenant.</returns>
    Task<IReadOnlyList<OrchestrationWorkflow>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflows by status.
    /// </summary>
    /// <param name="status">The workflow status.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of workflows with the specified status.</returns>
    Task<IReadOnlyList<OrchestrationWorkflow>> GetByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default);
}

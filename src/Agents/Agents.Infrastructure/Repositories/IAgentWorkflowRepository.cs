// <copyright file="IAgentWorkflowRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Infrastructure.Repositories;

using Synaxis.Agents.Domain.Aggregates;

/// <summary>
/// Repository for agent workflow aggregates.
/// </summary>
public interface IAgentWorkflowRepository
{
    /// <summary>
    /// Gets an agent workflow by ID.
    /// </summary>
    /// <param name="id">The workflow identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent workflow if found; otherwise, <see langword="null"/>.</returns>
    Task<AgentWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an agent workflow.
    /// </summary>
    /// <param name="workflow">The agent workflow to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAsync(AgentWorkflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets agent workflows by tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of agent workflows for the specified tenant.</returns>
    Task<IReadOnlyList<AgentWorkflow>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

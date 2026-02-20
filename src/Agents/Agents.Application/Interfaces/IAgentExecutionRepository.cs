// <copyright file="IAgentExecutionRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Interfaces;

using Synaxis.Agents.Domain.Aggregates;

/// <summary>
/// Repository for agent execution aggregates.
/// </summary>
public interface IAgentExecutionRepository
{
    /// <summary>
    /// Gets an agent execution by ID.
    /// </summary>
    /// <param name="id">The execution identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent execution if found; otherwise, <see langword="null"/>.</returns>
    Task<AgentExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets agent executions by agent ID.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of agent executions for the specified agent.</returns>
    Task<IReadOnlyList<AgentExecution>> GetByAgentIdAsync(Guid agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an agent execution.
    /// </summary>
    /// <param name="execution">The agent execution to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAsync(AgentExecution execution, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all running executions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of running agent executions.</returns>
    Task<IReadOnlyList<AgentExecution>> GetRunningExecutionsAsync(CancellationToken cancellationToken = default);
}

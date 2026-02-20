// <copyright file="IAgentExecutionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Interfaces;

using Synaxis.Agents.Domain.Aggregates;

/// <summary>
/// Service for executing agents.
/// </summary>
public interface IAgentExecutionService
{
    /// <summary>
    /// Starts execution of an agent.
    /// </summary>
    /// <param name="execution">The agent execution aggregate.</param>
    /// <param name="configuration">The agent configuration.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartExecutionAsync(
        AgentExecution execution,
        AgentConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an ongoing execution.
    /// </summary>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CancelExecutionAsync(string executionId, CancellationToken cancellationToken = default);
}

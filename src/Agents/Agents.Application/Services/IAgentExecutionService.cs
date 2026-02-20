// <copyright file="IAgentExecutionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Services;

using Synaxis.Agents.Application.DTOs;

/// <summary>
/// Service for managing agent execution lifecycle at the application layer.
/// </summary>
public interface IAgentExecutionService
{
    /// <summary>
    /// Starts a new execution for an agent.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <param name="parameters">The input parameters for the execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created execution DTO.</returns>
    Task<AgentExecutionDto> StartExecutionAsync(
        Guid agentId,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an ongoing execution.
    /// </summary>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated execution DTO.</returns>
    Task<AgentExecutionDto> CancelExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an execution by ID.
    /// </summary>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The execution DTO if found; otherwise, null.</returns>
    Task<AgentExecutionDto?> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions for a specific agent.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of execution DTOs.</returns>
    Task<IReadOnlyList<AgentExecutionDto>> GetExecutionsAsync(
        Guid agentId,
        CancellationToken cancellationToken = default);
}

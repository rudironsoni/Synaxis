// <copyright file="IAgentConfigurationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Services;

using Synaxis.Agents.Application.DTOs;

/// <summary>
/// Service for managing agent configuration lifecycle.
/// </summary>
public interface IAgentConfigurationService
{
    /// <summary>
    /// Creates a new agent configuration.
    /// </summary>
    /// <param name="request">The create agent request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created agent DTO.</returns>
    Task<AgentDto> CreateAgentAsync(
        CreateAgentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing agent configuration.
    /// </summary>
    /// <param name="id">The agent identifier.</param>
    /// <param name="request">The update agent request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated agent DTO.</returns>
    Task<AgentDto> UpdateAgentAsync(
        Guid id,
        UpdateAgentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an agent configuration.
    /// </summary>
    /// <param name="id">The agent identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAgentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an agent by ID.
    /// </summary>
    /// <param name="id">The agent identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent DTO if found; otherwise, null.</returns>
    Task<AgentDto?> GetAgentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets agents with pagination and filtering.
    /// </summary>
    /// <param name="request">The get agents request with pagination.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated result of agent DTOs.</returns>
    Task<PaginatedResult<AgentDto>> GetAgentsAsync(
        GetAgentsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates an agent.
    /// </summary>
    /// <param name="id">The agent identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated agent DTO.</returns>
    Task<AgentDto> ActivateAgentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an agent.
    /// </summary>
    /// <param name="id">The agent identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated agent DTO.</returns>
    Task<AgentDto> DeactivateAgentAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

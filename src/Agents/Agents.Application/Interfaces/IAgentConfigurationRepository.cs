// <copyright file="IAgentConfigurationRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Interfaces;

using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Repository for agent configuration aggregates.
/// </summary>
public interface IAgentConfigurationRepository
{
    /// <summary>
    /// Gets an agent configuration by ID.
    /// </summary>
    /// <param name="id">The agent configuration identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent configuration if found; otherwise, <see langword="null"/>.</returns>
    Task<AgentConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an agent configuration.
    /// </summary>
    /// <param name="configuration">The agent configuration to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets agent configurations by tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of agent configurations for the specified tenant.</returns>
    Task<IReadOnlyList<AgentConfiguration>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets agent configurations by status.
    /// </summary>
    /// <param name="status">The agent status.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of agent configurations with the specified status.</returns>
    Task<IReadOnlyList<AgentConfiguration>> GetByStatusAsync(AgentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an agent configuration by ID.
    /// </summary>
    /// <param name="id">The agent configuration identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

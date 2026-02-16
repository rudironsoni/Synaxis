// <copyright file="AgentOrchestrator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Infrastructure.Orchestration;

using System.Collections.Concurrent;
using System.Threading;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;
using Synaxis.Agents.Infrastructure.Parsing;

/// <summary>
/// Service to execute declarative agents and manage their lifecycle.
/// </summary>
public sealed class AgentOrchestrator : IDisposable
{
    private readonly ConcurrentDictionary<Guid, AgentConfiguration> _activeAgents;
    private readonly SemaphoreSlim _executionLock;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentOrchestrator"/> class.
    /// </summary>
    public AgentOrchestrator()
    {
        this._activeAgents = new ConcurrentDictionary<Guid, AgentConfiguration>();
        this._executionLock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Loads an agent configuration from a YAML string.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="name">The name of the agent.</param>
    /// <param name="description">The description of the agent.</param>
    /// <param name="yaml">The YAML configuration string.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="teamId">The team identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The loaded AgentConfiguration aggregate.</returns>
    public AgentConfiguration LoadAgent(
        Guid agentId,
        string name,
        string? description,
        string yaml,
        Guid tenantId,
        Guid? teamId,
        Guid? userId)
    {
        var agent = AgentConfiguration.Create(
            agentId,
            name,
            description,
            "declarative",
            yaml,
            tenantId,
            teamId,
            userId);

        this._activeAgents.TryAdd(agentId, agent);
        return agent;
    }

    /// <summary>
    /// Loads an agent configuration from a YAML file.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="name">The name of the agent.</param>
    /// <param name="description">The description of the agent.</param>
    /// <param name="filePath">The path to the YAML file.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="teamId">The team identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The loaded AgentConfiguration aggregate.</returns>
    public async Task<AgentConfiguration> LoadAgentAsync(
        Guid agentId,
        string name,
        string? description,
        string filePath,
        Guid tenantId,
        Guid? teamId,
        Guid? userId)
    {
        var yaml = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        return this.LoadAgent(agentId, name, description, yaml, tenantId, teamId, userId);
    }

    /// <summary>
    /// Starts the execution of an agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        await this._executionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!this._activeAgents.TryGetValue(agentId, out var agent))
            {
                throw new InvalidOperationException($"Agent not found: {agentId}");
            }

            agent.Activate();
        }
        finally
        {
            this._executionLock.Release();
        }
    }

    /// <summary>
    /// Pauses the execution of an agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PauseAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        await this._executionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!this._activeAgents.TryGetValue(agentId, out var agent))
            {
                throw new InvalidOperationException($"Agent not found: {agentId}");
            }

            agent.Deactivate();
        }
        finally
        {
            this._executionLock.Release();
        }
    }

    /// <summary>
    /// Resumes the execution of a paused agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ResumeAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return this.StartAgentAsync(agentId, cancellationToken);
    }

    /// <summary>
    /// Stops the execution of an agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StopAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return this.PauseAgentAsync(agentId, cancellationToken);
    }

    /// <summary>
    /// Gets an active agent by its identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <returns>The AgentConfiguration aggregate, or null if not found.</returns>
    public AgentConfiguration? GetAgent(Guid agentId)
    {
        return this._activeAgents.TryGetValue(agentId, out var agent) ? agent : null;
    }

    /// <summary>
    /// Gets all active agents.
    /// </summary>
    /// <returns>A read-only list of all active agents.</returns>
    public IReadOnlyList<AgentConfiguration> GetAllAgents()
    {
        return this._activeAgents.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Removes an agent from the active agents collection.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <returns>True if the agent was removed; otherwise, false.</returns>
    public bool RemoveAgent(Guid agentId)
    {
        return this._activeAgents.TryRemove(agentId, out _);
    }

    /// <summary>
    /// Disposes the orchestrator and releases resources.
    /// </summary>
    public void Dispose()
    {
        this._executionLock.Dispose();
    }
}

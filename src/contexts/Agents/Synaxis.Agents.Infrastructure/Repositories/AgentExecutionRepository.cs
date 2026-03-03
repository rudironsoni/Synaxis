// <copyright file="AgentExecutionRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Infrastructure.Repositories;

using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.Aggregates;

/// <summary>
/// Implementation of agent execution repository using event sourcing.
/// </summary>
public class AgentExecutionRepository : IAgentExecutionRepository
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<AgentExecutionRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentExecutionRepository"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="logger">The logger.</param>
    public AgentExecutionRepository(IEventStore eventStore, ILogger<AgentExecutionRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(logger);
        this._eventStore = eventStore;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AgentExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var events = await this._eventStore.ReadStreamAsync(id.ToString(), cancellationToken).ConfigureAwait(false);
        if (events.Count == 0)
        {
            return null;
        }

        var execution = new AgentExecution();
        execution.LoadFromHistory(events);

        return execution;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(AgentExecution execution, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(execution);

        var uncommittedEvents = execution.GetUncommittedEvents();
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var expectedVersion = execution.Version - uncommittedEvents.Count;

        await this._eventStore.AppendAsync(
            execution.Id.ToString(),
            uncommittedEvents,
            expectedVersion,
            cancellationToken).ConfigureAwait(false);

        execution.MarkAsCommitted();

        this._logger.LogDebug(
            "Saved agent execution {ExecutionId} with {EventCount} events",
            execution.Id,
            uncommittedEvents.Count);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AgentExecution>> GetByAgentIdAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AgentExecution>>(new List<AgentExecution>());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AgentExecution>> GetRunningExecutionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AgentExecution>>(new List<AgentExecution>());
    }
}

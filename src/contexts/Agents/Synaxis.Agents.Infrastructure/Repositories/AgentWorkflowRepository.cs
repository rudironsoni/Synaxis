// <copyright file="AgentWorkflowRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Infrastructure.Repositories;

using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.Aggregates;

/// <summary>
/// Implementation of agent workflow repository using event sourcing.
/// </summary>
public class AgentWorkflowRepository : IAgentWorkflowRepository
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<AgentWorkflowRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentWorkflowRepository"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="logger">The logger.</param>
    public AgentWorkflowRepository(IEventStore eventStore, ILogger<AgentWorkflowRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(logger);
        this._eventStore = eventStore;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AgentWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var events = await this._eventStore.ReadStreamAsync(id.ToString(), cancellationToken).ConfigureAwait(false);
        if (events.Count == 0)
        {
            return null;
        }

        var workflow = new AgentWorkflow();
        workflow.LoadFromHistory(events);

        return workflow;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(AgentWorkflow workflow, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var uncommittedEvents = workflow.GetUncommittedEvents();
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var expectedVersion = workflow.Version - uncommittedEvents.Count;

        await this._eventStore.AppendAsync(
            workflow.Id.ToString(),
            uncommittedEvents,
            expectedVersion,
            cancellationToken).ConfigureAwait(false);

        workflow.MarkAsCommitted();

        this._logger.LogDebug(
            "Saved agent workflow {WorkflowId} with {EventCount} events",
            workflow.Id,
            uncommittedEvents.Count);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AgentWorkflow>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AgentWorkflow>>(new List<AgentWorkflow>());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AgentWorkflow>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Event-sourced repository without read model - return empty list for now
        return Task.FromResult<IReadOnlyList<AgentWorkflow>>(new List<AgentWorkflow>());
    }
}

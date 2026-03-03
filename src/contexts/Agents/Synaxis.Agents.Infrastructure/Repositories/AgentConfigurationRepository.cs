// <copyright file="AgentConfigurationRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Infrastructure.Repositories;

using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Implementation of agent configuration repository using event sourcing.
/// </summary>
public class AgentConfigurationRepository : IAgentConfigurationRepository
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<AgentConfigurationRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigurationRepository"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="logger">The logger.</param>
    public AgentConfigurationRepository(IEventStore eventStore, ILogger<AgentConfigurationRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(logger);
        this._eventStore = eventStore;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AgentConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var events = await this._eventStore.ReadStreamAsync(id.ToString(), cancellationToken).ConfigureAwait(false);
        if (events.Count == 0)
        {
            return null;
        }

        var configuration = new AgentConfiguration();
        configuration.LoadFromHistory(events);

        return configuration;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var uncommittedEvents = configuration.GetUncommittedEvents();
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var expectedVersion = configuration.Version - uncommittedEvents.Count;

        await this._eventStore.AppendAsync(
            configuration.Id.ToString(),
            uncommittedEvents,
            expectedVersion,
            cancellationToken).ConfigureAwait(false);

        configuration.MarkAsCommitted();

        this._logger.LogDebug(
            "Saved agent configuration {AgentId} with {EventCount} events",
            configuration.Id,
            uncommittedEvents.Count);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AgentConfiguration>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AgentConfiguration>>(new List<AgentConfiguration>());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AgentConfiguration>> GetByStatusAsync(AgentStatus status, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AgentConfiguration>>(new List<AgentConfiguration>());
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await this._eventStore.DeleteAsync(id.ToString(), cancellationToken).ConfigureAwait(false);

        this._logger.LogDebug("Deleted agent configuration {AgentId}", id);
    }
}

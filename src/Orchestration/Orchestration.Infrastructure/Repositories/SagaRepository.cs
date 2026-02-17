// <copyright file="SagaRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure.Repositories;

using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Orchestration.Domain;
using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Implementation of saga repository using event sourcing.
/// </summary>
public class SagaRepository : ISagaRepository
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<SagaRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaRepository"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="logger">The logger.</param>
    public SagaRepository(IEventStore eventStore, ILogger<SagaRepository> logger)
    {
        this._eventStore = eventStore;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Saga?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var events = await this._eventStore.ReadStreamAsync(id.ToString(), cancellationToken).ConfigureAwait(false);
        if (events.Count == 0)
        {
            return null;
        }

        var saga = new Saga();
        saga.LoadFromHistory(events);

        return saga;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Saga saga, CancellationToken cancellationToken = default)
    {
        var uncommittedEvents = saga.GetUncommittedEvents();
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var expectedVersion = saga.Version - uncommittedEvents.Count;

        await this._eventStore.AppendAsync(
            saga.Id.ToString(),
            uncommittedEvents,
            expectedVersion,
            cancellationToken).ConfigureAwait(false);

        saga.MarkAsCommitted();

        this._logger.LogDebug(
            "Saved saga {SagaId} with {EventCount} events",
            saga.Id,
            uncommittedEvents.Count);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Saga>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Saga>>(new List<Saga>());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Saga>> GetByStatusAsync(SagaStatus status, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Saga>>(new List<Saga>());
    }
}

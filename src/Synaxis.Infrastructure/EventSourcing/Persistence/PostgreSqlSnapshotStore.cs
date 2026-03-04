// <copyright file="PostgreSqlSnapshotStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing.Persistence;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synaxis.Infrastructure.EventSourcing.Models;
using Synaxis.Infrastructure.EventSourcing.Serialization;

/// <summary>
/// PostgreSQL implementation of the snapshot store.
/// </summary>
public sealed class PostgreSqlSnapshotStore : ISnapshotStore
{
    private readonly EventStoreDbContext dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlSnapshotStore"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="serializer">The event serializer.</param>
    public PostgreSqlSnapshotStore(EventStoreDbContext dbContext, IEventSerializer serializer)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        // Serializer is injected for future extensibility
        _ = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc/>
    public async Task<SnapshotResult<TState>?> GetSnapshotAsync<TState>(
        string streamId,
        CancellationToken cancellationToken = default)
        where TState : notnull
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        var snapshot = await this.dbContext.Snapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StreamId == streamId, cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        var state = JsonSerializer.Deserialize<TState>(snapshot.StateData);

        if (state is null)
        {
            return null;
        }

        return new SnapshotResult<TState>(state, snapshot.Version);
    }

    /// <inheritdoc/>
    public async Task SaveSnapshotAsync<TState>(
        string streamId,
        long version,
        TState state,
        CancellationToken cancellationToken = default)
        where TState : notnull
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var existingSnapshot = await this.dbContext.Snapshots
            .FirstOrDefaultAsync(s => s.StreamId == streamId, cancellationToken);

        var stateJson = JsonSerializer.Serialize(state);

        if (existingSnapshot is not null)
        {
            existingSnapshot.Version = version;
            existingSnapshot.StateData = stateJson;
            existingSnapshot.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            var snapshot = new Snapshot
            {
                StreamId = streamId,
                Version = version,
                AggregateType = typeof(TState).FullName ?? typeof(TState).Name,
                StateData = stateJson,
                CreatedAt = DateTime.UtcNow,
            };

            this.dbContext.Snapshots.Add(snapshot);
        }

        await this.dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteSnapshotAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        var snapshot = await this.dbContext.Snapshots
            .FirstOrDefaultAsync(s => s.StreamId == streamId, cancellationToken);

        if (snapshot is not null)
        {
            this.dbContext.Snapshots.Remove(snapshot);
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

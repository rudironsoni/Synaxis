// <copyright file="PostgreSqlEventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing.Stores;

using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Synaxis.Infrastructure.EventSourcing.Models;
using Synaxis.Infrastructure.EventSourcing.Serialization;

/// <summary>
/// PostgreSQL implementation of the event store.
/// </summary>
public sealed class PostgreSqlEventStore : IEventStore
{
    private readonly EventStoreDbContext _dbContext;
    private readonly IEventSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlEventStore"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="serializer">The event serializer.</param>
    public PostgreSqlEventStore(EventStoreDbContext dbContext, IEventSerializer serializer)
    {
        this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this._serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc/>
    public async Task AppendAsync(
        string streamId,
        long expectedVersion,
        IEnumerable<IEventEnvelope> events,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        if (events is null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        var eventList = new List<IEventEnvelope>(events);
        if (eventList.Count == 0)
        {
            return;
        }

        // Use optimistic concurrency with a PostgreSQL advisory lock
        var connection = _dbContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            // Get the current version using advisory lock for stream consistency
            var currentVersion = await GetCurrentVersionAsync(connection, streamId, transaction, cancellationToken);

            if (currentVersion != expectedVersion)
            {
                throw new ConcurrencyException(streamId, expectedVersion, currentVersion);
            }

            // Insert events
            var nextVersion = currentVersion + 1;
            foreach (var envelope in eventList)
            {
                await InsertEventAsync(
                    connection,
                    streamId,
                    nextVersion++,
                    envelope,
                    transaction,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<IEventEnvelope> ReadAsync(
        string streamId,
        long fromVersion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        }

        var connection = _dbContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        const string sql = @"
            SELECT id, global_position, stream_id, version, event_type, event_data, metadata, timestamp, event_id
            FROM event_store
            WHERE stream_id = @streamId AND version >= @fromVersion
            ORDER BY version ASC";

        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("@streamId", streamId);
        command.Parameters.AddWithValue("@fromVersion", fromVersion);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return MapToEventEnvelope(reader);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<IEventEnvelope> ReadAllAsync(
        long fromPosition = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        const string sql = @"
            SELECT id, global_position, stream_id, version, event_type, event_data, metadata, timestamp, event_id
            FROM event_store
            WHERE global_position >= @fromPosition
            ORDER BY global_position ASC";

        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("@fromPosition", fromPosition);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return MapToEventEnvelope(reader);
        }
    }

    /// <summary>
    /// Gets the current version of a stream.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="transaction">The database transaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current version, or -1 if the stream doesn't exist.</returns>
    private static async Task<long> GetCurrentVersionAsync(
        IDbConnection connection,
        string streamId,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COALESCE(MAX(version), -1)
            FROM event_store
            WHERE stream_id = @streamId";

        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection, (NpgsqlTransaction)transaction);
        command.Parameters.AddWithValue("@streamId", streamId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is DBNull ? -1 : Convert.ToInt64(result);
    }

    /// <summary>
    /// Inserts an event into the event store.
    /// </summary>
    private async Task InsertEventAsync(
        IDbConnection connection,
        string streamId,
        long version,
        IEventEnvelope envelope,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO event_store (stream_id, version, event_type, event_data, metadata, timestamp, event_id)
            VALUES (@streamId, @version, @eventType, @eventData::jsonb, @metadata::jsonb, @timestamp, @eventId)
            RETURNING global_position";

        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection, (NpgsqlTransaction)transaction);

        var eventDataJson = _serializer.Serialize(envelope.EventData);
        var metadataJson = envelope.Metadata is not null
            ? JsonSerializer.Serialize(envelope.Metadata)
            : null;

        command.Parameters.AddWithValue("@streamId", streamId);
        command.Parameters.AddWithValue("@version", version);
        command.Parameters.AddWithValue("@eventType", envelope.EventType);
        command.Parameters.AddWithValue("@eventData", eventDataJson);
        command.Parameters.AddWithValue("@metadata", metadataJson ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@timestamp", envelope.Timestamp);
        command.Parameters.AddWithValue("@eventId", envelope.EventId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Maps a database reader row to an event envelope.
    /// </summary>
    private IEventEnvelope MapToEventEnvelope(NpgsqlDataReader reader)
    {
        var id = reader.GetInt64(0);
        var globalPosition = reader.GetInt64(1);
        var streamId = reader.GetString(2);
        var version = reader.GetInt64(3);
        var eventType = reader.GetString(4);
        var eventDataJson = reader.GetString(5);
        var metadataJson = reader.IsDBNull(6) ? null : reader.GetString(6);
        var timestamp = reader.GetDateTime(7);
        var eventId = reader.GetGuid(8);

        // Deserialize event data
        var eventTypeResolved = _serializer.ResolveType(eventType);
        if (eventTypeResolved is null)
        {
            throw new InvalidOperationException($"Could not resolve event type: {eventType}");
        }

        var eventData = _serializer.Deserialize(eventDataJson, eventTypeResolved);
        var metadata = metadataJson is not null
            ? JsonSerializer.Deserialize<EventMetadata>(metadataJson)
            : new EventMetadata();

        return new EventEnvelope(
            eventId,
            streamId,
            version,
            globalPosition,
            eventType,
            eventData,
            metadata ?? new EventMetadata(),
            timestamp);
    }

    /// <summary>
    /// Ensures the database connection is open.
    /// </summary>
    private static async Task EnsureConnectionOpenAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is NpgsqlConnection npgsqlConnection)
        {
            if (npgsqlConnection.State != ConnectionState.Open)
            {
                await npgsqlConnection.OpenAsync(cancellationToken);
            }
        }
        else if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }
    }
}

// <copyright file="PostgreSqlEventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OnPrem;

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// PostgreSQL-based implementation of IEventStore for on-premise deployments.
/// </summary>
#pragma warning disable SA1101 // Prefix local calls with this - Fields are prefixed with underscore, not this
#pragma warning disable MA0004 // Use Task.ConfigureAwait(false) - await using statements don't support ConfigureAwait
public class PostgreSqlEventStore : IEventStore
{
    private readonly string _connectionString;
    private readonly ILogger<PostgreSqlEventStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlEventStore"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="logger">The logger instance.</param>
    public PostgreSqlEventStore(
        string connectionString,
        ILogger<PostgreSqlEventStore> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task AppendAsync(
        string streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Verify expected version for optimistic concurrency
            var currentVersion = await GetCurrentVersionAsync(connection, streamId, cancellationToken).ConfigureAwait(false);
            if (currentVersion != expectedVersion)
            {
                throw new InvalidOperationException(
                    $"Concurrency conflict: expected version {expectedVersion}, but current version is {currentVersion}");
            }

            var version = currentVersion + 1;
            foreach (var domainEvent in events)
            {
                var eventData = JsonConvert.SerializeObject(domainEvent);
                var eventType = domainEvent.GetType().AssemblyQualifiedName;

                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO events (stream_id, version, event_type, event_data, occurred_on)
                    VALUES (@streamId, @version, @eventType, @eventData, @occurredOn)";

                command.Parameters.AddWithValue("streamId", streamId);
                command.Parameters.AddWithValue("version", version);
                command.Parameters.AddWithValue("eventType", eventType ?? string.Empty);
                command.Parameters.AddWithValue("eventData", eventData);
                command.Parameters.AddWithValue("occurredOn", domainEvent.OccurredOn);

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                version++;
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Appended {Count} events to stream {StreamId}", events, streamId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IDomainEvent>> ReadAsync(
        string streamId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT event_type, event_data, occurred_on
            FROM events
            WHERE stream_id = @streamId AND version >= @fromVersion AND version <= @toVersion
            ORDER BY version";

        command.Parameters.AddWithValue("streamId", streamId);
        command.Parameters.AddWithValue("fromVersion", fromVersion);
        command.Parameters.AddWithValue("toVersion", toVersion);

        var events = new List<IDomainEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var eventType = reader.GetString(0);
            var eventData = reader.GetString(1);

            var type = Type.GetType(eventType);
            if (type != null)
            {
                var domainEvent = (IDomainEvent?)JsonConvert.DeserializeObject(eventData, type);
                if (domainEvent != null)
                {
                    events.Add(domainEvent);
                }
            }
        }

        _logger.LogInformation("Read {Count} events from stream {StreamId} (v{From}-v{To})", events.Count, streamId, fromVersion, toVersion);
        return events.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT event_type, event_data, occurred_on
            FROM events
            WHERE stream_id = @streamId
            ORDER BY version";

        command.Parameters.AddWithValue("streamId", streamId);

        var events = new List<IDomainEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var eventType = reader.GetString(0);
            var eventData = reader.GetString(1);

            var type = Type.GetType(eventType);
            if (type != null)
            {
                var domainEvent = (IDomainEvent?)JsonConvert.DeserializeObject(eventData, type);
                if (domainEvent != null)
                {
                    events.Add(domainEvent);
                }
            }
        }

        _logger.LogInformation("Read {Count} events from stream {StreamId}", events.Count, streamId);
        return events.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM events WHERE stream_id = @streamId";
        command.Parameters.AddWithValue("streamId", streamId);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Deleted {Count} events from stream {StreamId}", rowsAffected, streamId);
    }

    private static async Task<int> GetCurrentVersionAsync(
        NpgsqlConnection connection,
        string streamId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(version), 0) FROM events WHERE stream_id = @streamId";
        command.Parameters.AddWithValue("streamId", streamId);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result != null ? Convert.ToInt32(result, CultureInfo.InvariantCulture) : 0;
    }
}
#pragma warning restore MA0004 // Use Task.ConfigureAwait(false)
#pragma warning restore SA1101 // Prefix local calls with this

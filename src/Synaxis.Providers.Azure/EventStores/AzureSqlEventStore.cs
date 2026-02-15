// <copyright file="AzureSqlEventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Azure.EventStores;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using global::Polly;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Azure SQL implementation of IEventStore.
/// </summary>
public class AzureSqlEventStore : IEventStore
{
    private readonly string _connectionString;
    private readonly ILogger<AzureSqlEventStore> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureSqlEventStore"/> class.
    /// </summary>
    /// <param name="connectionString">The SQL connection string.</param>
    /// <param name="logger">The logger instance.</param>
    public AzureSqlEventStore(string connectionString, ILogger<AzureSqlEventStore> logger)
    {
        this._connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._retryPolicy = Policy
            .Handle<SqlException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    this._logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s",
                        retryCount,
                        timespan.TotalSeconds);
                });
    }

    /// <inheritdoc />
    public Task AppendAsync(
        string streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
#pragma warning disable MA0004 // await using doesn't support ConfigureAwait(false)
            await using var connection = new SqlConnection(this._connectionString);
#pragma warning restore MA0004
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

#pragma warning disable MA0004 // await using doesn't support ConfigureAwait(false)
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore MA0004

            try
            {
                var currentVersion = await GetCurrentVersionAsync(connection, transaction, streamId, cancellationToken).ConfigureAwait(false);
                if (currentVersion != expectedVersion)
                {
                    throw new InvalidOperationException(
                        $"Concurrency conflict: expected version {expectedVersion}, but current version is {currentVersion}");
                }

                var version = expectedVersion + 1;
                foreach (var domainEvent in events)
                {
                    using var command = new SqlCommand(
                        @"INSERT INTO Events (Id, StreamId, Version, EventType, Payload, Metadata, Timestamp)
                          VALUES (@Id, @StreamId, @Version, @EventType, @Payload, @Metadata, @Timestamp)",
                        connection,
                        transaction);

                    command.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    command.Parameters.AddWithValue("@StreamId", streamId);
                    command.Parameters.AddWithValue("@Version", version);
                    command.Parameters.AddWithValue("@EventType", domainEvent.EventType);
                    command.Parameters.AddWithValue("@Payload", JsonSerializer.Serialize(domainEvent));
                    command.Parameters.AddWithValue("@Metadata", JsonSerializer.Serialize(new { }));
                    command.Parameters.AddWithValue("@Timestamp", domainEvent.OccurredOn);

                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    version++;
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                this._logger.LogInformation(
                    "Appended {Count} events to stream {StreamId} starting at version {Version}",
                    events.Count(),
                    streamId,
                    expectedVersion + 1);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IDomainEvent>> ReadAsync(
        string streamId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default)
    {
        return await this._retryPolicy.ExecuteAsync(async () =>
        {
#pragma warning disable MA0004 // await using doesn't support ConfigureAwait(false)
            await using var connection = new SqlConnection(this._connectionString);
#pragma warning restore MA0004
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                @"SELECT Payload, EventType, Timestamp
                  FROM Events
                  WHERE StreamId = @StreamId AND Version >= @FromVersion AND Version <= @ToVersion
                  ORDER BY Version ASC",
                connection);

            command.Parameters.AddWithValue("@StreamId", streamId);
            command.Parameters.AddWithValue("@FromVersion", fromVersion);
            command.Parameters.AddWithValue("@ToVersion", toVersion);

            var events = new List<IDomainEvent>();
#pragma warning disable MA0004 // await using doesn't support ConfigureAwait(false)
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore MA0004

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var payload = reader.GetString(0);
                var eventType = reader.GetString(1);
                var timestamp = reader.GetDateTime(2);

                var eventWrapper = new DomainEventWrapper
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventType = eventType,
                    OccurredOn = timestamp,
                    Payload = payload,
                };

                events.Add(eventWrapper);
            }

            this._logger.LogInformation(
                "Read {Count} events from stream {StreamId} (version {FromVersion} to {ToVersion})",
                events.Count,
                streamId,
                fromVersion,
                toVersion);

            return events.AsReadOnly();
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
#pragma warning disable MA0004 // await using doesn't support ConfigureAwait(false)
            await using var connection = new SqlConnection(this._connectionString);
#pragma warning restore MA0004
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                @"SELECT MAX(Version) FROM Events WHERE StreamId = @StreamId",
                connection);

            command.Parameters.AddWithValue("@StreamId", streamId);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            var maxVersion = result as int? ?? 0;

            if (maxVersion == 0)
            {
                return Array.Empty<IDomainEvent>();
            }

            return await this.ReadAsync(streamId, 1, maxVersion, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
#pragma warning disable MA0004 // await using doesn't support ConfigureAwait(false)
            await using var connection = new SqlConnection(this._connectionString);
#pragma warning restore MA0004
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(
                @"DELETE FROM Events WHERE StreamId = @StreamId",
                connection);

            command.Parameters.AddWithValue("@StreamId", streamId);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            this._logger.LogInformation("Deleted stream {StreamId} ({Count} events)", streamId, rowsAffected);
        });
    }

    private static async Task<int> GetCurrentVersionAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string streamId,
        CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT COALESCE(MAX(Version), 0) FROM Events WHERE StreamId = @StreamId",
            connection,
            transaction);

        command.Parameters.AddWithValue("@StreamId", streamId);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result as int? ?? 0;
    }

    /// <summary>
    /// Wrapper class for domain events deserialized from JSON.
    /// </summary>
    private sealed class DomainEventWrapper : IDomainEvent
    {
        public string EventId { get; set; } = string.Empty;

        public DateTime OccurredOn { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;
    }
}

// <copyright file="SnapshotStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Azure.EventStores;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using global::Polly;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

/// <summary>
/// Snapshot store for periodic state snapshots using Azure Cosmos DB.
/// </summary>
public class SnapshotStore
{
    private readonly Container _container;
    private readonly ILogger<SnapshotStore> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotStore"/> class.
    /// </summary>
    /// <param name="container">The Cosmos DB container for snapshots.</param>
    /// <param name="logger">The logger instance.</param>
    public SnapshotStore(Container container, ILogger<SnapshotStore> logger)
    {
        this._container = container!;
        this._logger = logger!;
        this._retryPolicy = Policy
            .Handle<CosmosException>()
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

    /// <summary>
    /// Saves a snapshot for a stream.
    /// </summary>
    /// <typeparam name="TState">The type of the state to snapshot.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="version">The event version at which the snapshot was taken.</param>
    /// <param name="state">The state to snapshot.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SaveSnapshotAsync<TState>(
        string streamId,
        int version,
        TState state,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            var snapshot = new SnapshotDocument
            {
                Id = streamId,
                StreamId = streamId,
                Version = version,
                StateType = typeof(TState).Name,
                State = JsonSerializer.Serialize(state),
                Timestamp = DateTime.UtcNow,
            };

            await this._container.UpsertItemAsync(snapshot, new PartitionKey(streamId), cancellationToken: cancellationToken).ConfigureAwait(false);
            this._logger.LogInformation(
                "Saved snapshot for stream {StreamId} at version {Version}",
                streamId,
                version);
        });
    }

    /// <summary>
    /// Retrieves the latest snapshot for a stream.
    /// </summary>
    /// <typeparam name="TState">The type of the state to retrieve.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The snapshot state, or null if no snapshot exists.</returns>
    public Task<TState?> GetSnapshotAsync<TState>(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                var response = await this._container.ReadItemAsync<SnapshotDocument>(
                    streamId,
                    new PartitionKey(streamId),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var snapshot = response.Resource;
                if (!string.Equals(snapshot.StateType, typeof(TState).Name, StringComparison.Ordinal))
                {
                    this._logger.LogWarning(
                        "Snapshot type mismatch for stream {StreamId}: expected {ExpectedType}, found {ActualType}",
                        streamId,
                        typeof(TState).Name,
                        snapshot.StateType);
                    return default;
                }

                var state = JsonSerializer.Deserialize<TState>(snapshot.State);
                this._logger.LogInformation(
                    "Retrieved snapshot for stream {StreamId} at version {Version}",
                    streamId,
                    snapshot.Version);

                return state;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                this._logger.LogInformation(ex, "No snapshot found for stream {StreamId}", streamId);
                return default;
            }
        });
    }

    /// <summary>
    /// Deletes a snapshot for a stream.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteSnapshotAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                await this._container.DeleteItemAsync<SnapshotDocument>(
                    streamId,
                    new PartitionKey(streamId),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                this._logger.LogInformation("Deleted snapshot for stream {StreamId}", streamId);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                this._logger.LogInformation(ex, "No snapshot found to delete for stream {StreamId}", streamId);
            }
        });
    }

    /// <summary>
    /// Gets the version of the latest snapshot for a stream.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The snapshot version, or null if no snapshot exists.</returns>
    public async Task<int?> GetSnapshotVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await this._container.ReadItemAsync<SnapshotDocument>(
                streamId,
                new PartitionKey(streamId),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return response.Resource.Version;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Cosmos DB document model for snapshots.
    /// </summary>
    private sealed class SnapshotDocument
    {
        public string Id { get; set; } = string.Empty;

        public string StreamId { get; set; } = string.Empty;

        public int Version { get; set; }

        public string StateType { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }
    }
}

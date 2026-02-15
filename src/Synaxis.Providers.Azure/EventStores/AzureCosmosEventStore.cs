// <copyright file="AzureCosmosEventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using global::Polly;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;

namespace Synaxis.Providers.Azure.EventStores;

/// <summary>
/// Azure Cosmos DB implementation of IEventStore.
/// </summary>
public class AzureCosmosEventStore : IEventStore
{
    private readonly Container _container;
    private readonly ILogger<AzureCosmosEventStore> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCosmosEventStore"/> class.
    /// </summary>
    /// <param name="container">The Cosmos DB container for events.</param>
    /// <param name="logger">The logger instance.</param>
    public AzureCosmosEventStore(Container container, ILogger<AzureCosmosEventStore> logger)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPolicy = Policy
            .Handle<CosmosException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s",
                        retryCount,
                        timespan.TotalSeconds);
                });
    }

    /// <inheritdoc />
    public async Task AppendAsync(
        string streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            var currentVersion = await GetCurrentVersionAsync(streamId, cancellationToken);
            if (currentVersion != expectedVersion)
            {
                throw new InvalidOperationException(
                    $"Concurrency conflict: expected version {expectedVersion}, but current version is {currentVersion}");
            }

            var version = expectedVersion + 1;
            var tasks = new List<Task>();

            foreach (var domainEvent in events)
            {
                var eventDocument = new EventDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    StreamId = streamId,
                    Version = version,
                    EventType = domainEvent.EventType,
                    Payload = JsonSerializer.Serialize(domainEvent),
                    Metadata = JsonSerializer.Serialize(new { }),
                    Timestamp = domainEvent.OccurredOn
                };

                tasks.Add(_container.CreateItemAsync(eventDocument, new PartitionKey(streamId), cancellationToken: cancellationToken));
                version++;
            }

            await Task.WhenAll(tasks);
            _logger.LogInformation(
                "Appended {Count} events to stream {StreamId} starting at version {Version}",
                events.Count(),
                streamId,
                expectedVersion + 1);
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IDomainEvent>> ReadAsync(
        string streamId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var query = new QueryDefinition(
                "SELECT * FROM e WHERE e.StreamId = @StreamId AND e.Version >= @FromVersion AND e.Version <= @ToVersion ORDER BY e.Version ASC")
                .WithParameter("@StreamId", streamId)
                .WithParameter("@FromVersion", fromVersion)
                .WithParameter("@ToVersion", toVersion);

            var events = new List<IDomainEvent>();
            var iterator = _container.GetItemQueryIterator<EventDocument>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                foreach (var document in response)
                {
                    var eventWrapper = new DomainEventWrapper
                    {
                        EventId = document.Id,
                        EventType = document.EventType,
                        OccurredOn = document.Timestamp,
                        Payload = document.Payload
                    };

                    events.Add(eventWrapper);
                }
            }

            _logger.LogInformation(
                "Read {Count} events from stream {StreamId} (version {FromVersion} to {ToVersion})",
                events.Count,
                streamId,
                fromVersion,
                toVersion);

            return events.AsReadOnly();
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var query = new QueryDefinition(
                "SELECT VALUE MAX(e.Version) FROM e WHERE e.StreamId = @StreamId")
                .WithParameter("@StreamId", streamId);

            var iterator = _container.GetItemQueryIterator<int>(query);
            var maxVersion = 0;

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                maxVersion = response.FirstOrDefault();
            }

            if (maxVersion == 0)
            {
                return Array.Empty<IDomainEvent>();
            }

            return await ReadAsync(streamId, 1, maxVersion, cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            var query = new QueryDefinition(
                "SELECT * FROM e WHERE e.StreamId = @StreamId")
                .WithParameter("@StreamId", streamId);

            var iterator = _container.GetItemQueryIterator<EventDocument>(query);
            var deleteTasks = new List<Task>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                foreach (var document in response)
                {
                    deleteTasks.Add(_container.DeleteItemAsync<EventDocument>(
                        document.Id,
                        new PartitionKey(streamId),
                        cancellationToken: cancellationToken));
                }
            }

            await Task.WhenAll(deleteTasks);
            _logger.LogInformation("Deleted stream {StreamId} ({Count} events)", streamId, deleteTasks.Count);
        });
    }

    private async Task<int> GetCurrentVersionAsync(string streamId, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT VALUE MAX(e.Version) FROM e WHERE e.StreamId = @StreamId")
            .WithParameter("@StreamId", streamId);

        var iterator = _container.GetItemQueryIterator<int>(query);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            return response.FirstOrDefault();
        }

        return 0;
    }

    /// <summary>
    /// Cosmos DB document model for events.
    /// </summary>
    private sealed class EventDocument
    {
        public string Id { get; set; } = string.Empty;
        public string StreamId { get; set; } = string.Empty;
        public int Version { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public string Metadata { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
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

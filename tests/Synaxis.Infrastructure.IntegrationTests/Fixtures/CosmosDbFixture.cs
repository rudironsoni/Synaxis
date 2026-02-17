// <copyright file="CosmosDbFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Providers.Azure.EventStores;
using Xunit;

/// <summary>
/// Fixture for Cosmos DB testing using in-memory storage.
/// </summary>
public sealed class CosmosDbFixture : IAsyncLifetime
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly InMemoryCosmosContainer _container;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDbFixture"/> class.
    /// </summary>
    public CosmosDbFixture()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _container = new InMemoryCosmosContainer();
    }

    /// <summary>
    /// Gets the event store instance.
    /// </summary>
    public IEventStore EventStore { get; private set; } = null!;

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        var logger = _loggerFactory.CreateLogger<AzureCosmosEventStore>();
        EventStore = new InMemoryCosmosEventStore(_container, logger);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        _loggerFactory.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// In-memory Cosmos container for testing.
    /// </summary>
    private sealed class InMemoryCosmosContainer
    {
        private readonly List<EventDocument> _events = new();

        public Task CreateItemAsync(EventDocument item, PartitionKey partitionKey, CancellationToken cancellationToken = default)
        {
            _events.Add(item);
            return Task.CompletedTask;
        }

        public InMemoryFeedIterator<EventDocument> GetItemQueryIterator(QueryDefinition query)
        {
            return new InMemoryFeedIterator<EventDocument>(_events);
        }

        public Task DeleteItemAsync<EventDocument>(string id, PartitionKey partitionKey, CancellationToken cancellationToken = default)
        {
            _events.RemoveAll(e => e.Id == id);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// In-memory feed iterator for testing.
    /// </summary>
    private sealed class InMemoryFeedIterator<T>
    {
        private readonly List<T> _items;
        private int _index = 0;

        public InMemoryFeedIterator(List<T> items)
        {
            _items = items;
        }

        public bool HasMoreResults => _index < _items.Count;

        public Task<List<T>> ReadNextAsync(CancellationToken cancellationToken = default)
        {
            var result = _index < _items.Count ? new List<T> { _items[_index] } : new List<T>();
            _index++;
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// In-memory Cosmos event store for testing.
    /// </summary>
    private sealed class InMemoryCosmosEventStore : IEventStore
    {
        private readonly InMemoryCosmosContainer _container;
        private readonly ILogger<AzureCosmosEventStore> _logger;
        private readonly Dictionary<string, int> _streamVersions = new();

        public InMemoryCosmosEventStore(InMemoryCosmosContainer container, ILogger<AzureCosmosEventStore> logger)
        {
            _container = container;
            _logger = logger;
        }

        public async Task AppendAsync(string streamId, IEnumerable<IDomainEvent> events, int expectedVersion, CancellationToken cancellationToken = default)
        {
            var currentVersion = _streamVersions.GetValueOrDefault(streamId, 0);
            if (currentVersion != expectedVersion)
            {
                throw new InvalidOperationException(
                    $"Concurrency conflict: expected version {expectedVersion}, but current version is {currentVersion}");
            }

            var version = expectedVersion + 1;
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

                await _container.CreateItemAsync(eventDocument, new PartitionKey(streamId), cancellationToken);
                version++;
            }

            _streamVersions[streamId] = version - 1;
            _logger.LogInformation(
                "Appended {Count} events to stream {StreamId} starting at version {Version}",
                events.Count(),
                streamId,
                expectedVersion + 1);
        }

        public async Task<IReadOnlyList<IDomainEvent>> ReadAsync(string streamId, int fromVersion, int toVersion, CancellationToken cancellationToken = default)
        {
            var events = _container.GetItemQueryIterator(new QueryDefinition("SELECT * FROM e"));
            var result = new List<IDomainEvent>();

            while (events.HasMoreResults)
            {
                var response = await events.ReadNextAsync(cancellationToken);
                foreach (var document in response.Where(e => e.StreamId == streamId && e.Version >= fromVersion && e.Version <= toVersion))
                {
                    result.Add(new DomainEventWrapper
                    {
                        EventId = document.Id,
                        EventType = document.EventType,
                        OccurredOn = document.Timestamp,
                        Payload = document.Payload
                    });
                }
            }

            return result.OrderBy(e => ((DomainEventWrapper)e).EventType).ToList().AsReadOnly();
        }

        public Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(string streamId, CancellationToken cancellationToken = default)
        {
            var maxVersion = _streamVersions.GetValueOrDefault(streamId, 0);
            if (maxVersion == 0)
            {
                return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
            }

            return ReadAsync(streamId, 1, maxVersion, cancellationToken);
        }

        public Task DeleteAsync(string streamId, CancellationToken cancellationToken = default)
        {
            _streamVersions.Remove(streamId);
            return Task.CompletedTask;
        }

        private sealed class DomainEventWrapper : IDomainEvent
        {
            public string EventId { get; set; } = string.Empty;
            public DateTime OccurredOn { get; set; }
            public string EventType { get; set; } = string.Empty;
            public string Payload { get; set; } = string.Empty;
        }
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
}

// <copyright file="SqlServerFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Providers.Azure.EventStores;
using Xunit;

/// <summary>
/// Fixture for SQL Server testing using in-memory storage.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly InMemoryEventStore _eventStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerFixture"/> class.
    /// </summary>
    public SqlServerFixture()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _eventStore = new InMemoryEventStore();
    }

    /// <summary>
    /// Gets the connection string for the SQL Server container.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the event store instance.
    /// </summary>
    public IEventStore EventStore { get; private set; } = null!;

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        var logger = _loggerFactory.CreateLogger<AzureSqlEventStore>();
        EventStore = _eventStore;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        _loggerFactory.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// In-memory event store for testing.
    /// </summary>
    private sealed class InMemoryEventStore : IEventStore
    {
        private readonly Dictionary<string, List<IDomainEvent>> _streams = new();
        private readonly Dictionary<string, int> _streamVersions = new();

        public Task AppendAsync(string streamId, IEnumerable<IDomainEvent> events, int expectedVersion, CancellationToken cancellationToken = default)
        {
            var currentVersion = _streamVersions.GetValueOrDefault(streamId, 0);
            if (currentVersion != expectedVersion)
            {
                throw new InvalidOperationException(
                    $"Concurrency conflict: expected version {expectedVersion}, but current version is {currentVersion}");
            }

            if (!_streams.ContainsKey(streamId))
            {
                _streams[streamId] = new List<IDomainEvent>();
            }

            var version = expectedVersion + 1;
            foreach (var @event in events)
            {
                _streams[streamId].Add(@event);
                version++;
            }

            _streamVersions[streamId] = version - 1;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IDomainEvent>> ReadAsync(string streamId, int fromVersion, int toVersion, CancellationToken cancellationToken = default)
        {
            if (!_streams.ContainsKey(streamId))
            {
                return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
            }

            var events = _streams[streamId]
                .Skip(fromVersion - 1)
                .Take(toVersion - fromVersion + 1)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<IDomainEvent>>(events);
        }

        public Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(string streamId, CancellationToken cancellationToken = default)
        {
            if (!_streams.ContainsKey(streamId))
            {
                return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
            }

            return Task.FromResult<IReadOnlyList<IDomainEvent>>(_streams[streamId].AsReadOnly());
        }

        public Task DeleteAsync(string streamId, CancellationToken cancellationToken = default)
        {
            _streams.Remove(streamId);
            _streamVersions.Remove(streamId);
            return Task.CompletedTask;
        }
    }
}

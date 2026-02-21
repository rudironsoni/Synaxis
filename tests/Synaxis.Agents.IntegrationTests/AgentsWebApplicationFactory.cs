// <copyright file="AgentsWebApplicationFactory.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.IntegrationTests;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synaxis.Abstractions.Cloud;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Thread-safe event store for integration tests using instance-based backing storage.
/// Each test class gets its own isolated instance via IClassFixture.
/// </summary>
public class TestEventStore : EventStore
{
    private readonly ConcurrentDictionary<string, List<IDomainEvent>> Streams = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> StreamVersions = new(StringComparer.Ordinal);

    public override Task AppendAsync(
        string streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        if (eventList.Count == 0)
        {
            return Task.CompletedTask;
        }

        // Optimistic concurrency check
        var currentVersion = StreamVersions.GetOrAdd(streamId, 0);
        if (currentVersion != expectedVersion)
        {
            throw new ConcurrencyException(streamId, expectedVersion, currentVersion);
        }

        var stream = Streams.GetOrAdd(streamId, _ => new List<IDomainEvent>());

        lock (stream)
        {
            stream.AddRange(eventList);
        }

        // Update version
        StreamVersions.AddOrUpdate(streamId, eventList.Count, (_, v) => v + eventList.Count);

        return Task.CompletedTask;
    }

    public override Task<IReadOnlyList<IDomainEvent>> ReadAsync(
        string streamId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default)
    {
        if (!Streams.TryGetValue(streamId, out var stream))
        {
            return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
        }

        lock (stream)
        {
            var result = stream
                .Skip(fromVersion)
                .Take(toVersion - fromVersion + 1)
                .ToList();
            return Task.FromResult<IReadOnlyList<IDomainEvent>>(result);
        }
    }

    public override Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (!Streams.TryGetValue(streamId, out var stream))
        {
            return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
        }

        lock (stream)
        {
            return Task.FromResult<IReadOnlyList<IDomainEvent>>(stream.ToList());
        }
    }

    public override Task DeleteAsync(string streamId, CancellationToken cancellationToken = default)
    {
        Streams.TryRemove(streamId, out _);
        StreamVersions.TryRemove(streamId, out _);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        Streams.Clear();
        StreamVersions.Clear();
    }
}

/// <summary>
/// Custom WebApplicationFactory for Agents integration tests.
/// Configures required services that are not registered in the main Program.cs.
/// </summary>
public class AgentsWebApplicationFactory : WebApplicationFactory<Agents.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IEventStore));
            services.AddSingleton<IEventStore, TestEventStore>();
        });
    }
}

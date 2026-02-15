// <copyright file="InMemoryEventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using System.Collections.Concurrent;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// In-memory implementation of <see cref="EventStore"/> for testing purposes.
/// </summary>
public class InMemoryEventStore : EventStore
{
    private readonly ConcurrentDictionary<string, List<IDomainEvent>> _streams = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public override Task AppendAsync(
        string streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace.", nameof(streamId));
        }

        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        var eventList = events.ToList();

        if (eventList.Count == 0)
        {
            return Task.CompletedTask;
        }

        this.ValidateConcurrency(streamId, expectedVersion);

        var stream = this._streams.GetOrAdd(streamId, _ => new List<IDomainEvent>());

        lock (stream)
        {
            stream.AddRange(eventList);
        }

        this.UpdateStreamVersion(streamId, eventList.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<IReadOnlyList<IDomainEvent>> ReadAsync(
        string streamId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace.", nameof(streamId));
        }

        if (fromVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fromVersion), "From version cannot be negative.");
        }

        if (toVersion < fromVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(toVersion), "To version must be greater than or equal to from version.");
        }

        if (!this._streams.TryGetValue(streamId, out var stream))
        {
            return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
        }

        List<IDomainEvent> result;

        lock (stream)
        {
            result = stream
                .Skip(fromVersion)
                .Take(toVersion - fromVersion + 1)
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<IDomainEvent>>(result);
    }

    /// <inheritdoc />
    public override Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace.", nameof(streamId));
        }

        if (!this._streams.TryGetValue(streamId, out var stream))
        {
            return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
        }

        IReadOnlyList<IDomainEvent> result;

        lock (stream)
        {
            result = stream.ToList().AsReadOnly();
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public override Task DeleteAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace.", nameof(streamId));
        }

        this._streams.TryRemove(streamId, out _);
        this.ResetStreamVersion(streamId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all streams from the in-memory event store.
    /// </summary>
    public void Clear()
    {
        this._streams.Clear();
    }
}

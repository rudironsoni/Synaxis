// <copyright file="EventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using System.Collections.Concurrent;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Abstract base class for event store implementations.
/// </summary>
public abstract class EventStore : IEventStore
{
    private readonly ConcurrentDictionary<string, int> _streamVersions = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public abstract Task AppendAsync(
        string streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<IReadOnlyList<IDomainEvent>> ReadAsync(
        string streamId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task DeleteAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the expected version against the current stream version for optimistic concurrency.
    /// </summary>
    /// <param name="streamId">The identifier of the event stream.</param>
    /// <param name="expectedVersion">The expected version of the stream.</param>
    /// <exception cref="ConcurrencyException">Thrown when the expected version does not match the current version.</exception>
    protected void ValidateConcurrency(string streamId, int expectedVersion)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace.", nameof(streamId));
        }

        var currentVersion = this._streamVersions.GetOrAdd(streamId, 0);

        if (currentVersion != expectedVersion)
        {
            throw new ConcurrencyException(streamId, expectedVersion, currentVersion);
        }
    }

    /// <summary>
    /// Updates the stream version after successfully appending events.
    /// </summary>
    /// <param name="streamId">The identifier of the event stream.</param>
    /// <param name="eventsAppended">The number of events that were appended.</param>
    protected void UpdateStreamVersion(string streamId, int eventsAppended)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace.", nameof(streamId));
        }

        this._streamVersions.AddOrUpdate(
            streamId,
            eventsAppended,
            (_, currentVersion) => currentVersion + eventsAppended);
    }

    /// <summary>
    /// Resets the stream version, typically after a stream is deleted.
    /// </summary>
    /// <param name="streamId">The identifier of the event stream.</param>
    protected void ResetStreamVersion(string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace.", nameof(streamId));
        }

        this._streamVersions.TryRemove(streamId, out _);
    }
}

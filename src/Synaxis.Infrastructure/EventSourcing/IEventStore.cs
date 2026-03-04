// <copyright file="IEventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for storing and retrieving events in an event sourcing system.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends events to a stream with optimistic concurrency control.
    /// </summary>
    /// <param name="streamId">The unique identifier of the event stream.</param>
    /// <param name="expectedVersion">The expected current version of the stream for concurrency checking.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ConcurrencyException">Thrown when the stream version does not match expectedVersion.</exception>
    Task AppendAsync(
        string streamId,
        long expectedVersion,
        IEnumerable<IEventEnvelope> events,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads events from a specific stream starting from a given version.
    /// </summary>
    /// <param name="streamId">The unique identifier of the event stream.</param>
    /// <param name="fromVersion">The version to start reading from (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of event envelopes ordered by version.</returns>
    IAsyncEnumerable<IEventEnvelope> ReadAsync(
        string streamId,
        long fromVersion = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all events from the store starting from a global position.
    /// </summary>
    /// <param name="fromPosition">The global position to start reading from (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of event envelopes ordered by global position.</returns>
    IAsyncEnumerable<IEventEnvelope> ReadAllAsync(
        long fromPosition = 0,
        CancellationToken cancellationToken = default);
}

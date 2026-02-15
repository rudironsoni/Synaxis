// <copyright file="IEventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines a contract for storing and retrieving domain events.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends a sequence of events to a stream with optimistic concurrency control.
    /// </summary>
    /// <param name="streamId">The unique identifier of the event stream.</param>
    /// <param name="events">The events to append to the stream.</param>
    /// <param name="expectedVersion">The expected version of the stream for optimistic concurrency.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AppendAsync(
        string streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads events from a stream within a version range.
    /// </summary>
    /// <param name="streamId">The unique identifier of the event stream.</param>
    /// <param name="fromVersion">The starting version to read from (inclusive).</param>
    /// <param name="toVersion">The ending version to read to (inclusive).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of events within the specified version range.</returns>
    Task<IReadOnlyList<IDomainEvent>> ReadAsync(
        string streamId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all events from a stream.
    /// </summary>
    /// <param name="streamId">The unique identifier of the event stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of all events in the stream.</returns>
    Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an event stream.
    /// </summary>
    /// <param name="streamId">The unique identifier of the event stream to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(
        string streamId,
        CancellationToken cancellationToken = default);
}

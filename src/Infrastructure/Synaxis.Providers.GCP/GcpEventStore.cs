// <copyright file="GcpEventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.GCP;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Stub implementation for future GCP EventStore integration using Firestore or Spanner.
/// </summary>
public class GcpEventStore : IEventStore
{
    /// <inheritdoc />
    public Task AppendAsync(
        string streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP EventStore integration is not yet implemented. This stub will use Firestore or Spanner for event storage.");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IDomainEvent>> ReadAsync(
        string streamId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP EventStore integration is not yet implemented. This stub will use Firestore or Spanner for event storage.");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP EventStore integration is not yet implemented. This stub will use Firestore or Spanner for event storage.");
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP EventStore integration is not yet implemented. This stub will use Firestore or Spanner for event storage.");
    }
}

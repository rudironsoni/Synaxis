// <copyright file="AwsEventStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.AWS;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Stub implementation for future AWS EventStore integration using DynamoDB or Kinesis.
/// </summary>
public class AwsEventStore : IEventStore
{
    /// <inheritdoc />
    public Task AppendAsync(
        string streamId,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS EventStore integration is not yet implemented. This stub will use DynamoDB or Kinesis for event storage.");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IDomainEvent>> ReadAsync(
        string streamId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS EventStore integration is not yet implemented. This stub will use DynamoDB or Kinesis for event storage.");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS EventStore integration is not yet implemented. This stub will use DynamoDB or Kinesis for event storage.");
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS EventStore integration is not yet implemented. This stub will use DynamoDB or Kinesis for event storage.");
    }
}

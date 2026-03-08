// <copyright file="NoSnapshotStrategy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing;

using Synaxis.Shared.Kernel.Application.Cloud;

/// <summary>
/// A snapshot strategy that never creates snapshots.
/// </summary>
public class NoSnapshotStrategy : ISnapshotStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoSnapshotStrategy"/> class.
    /// </summary>
    public NoSnapshotStrategy()
    {
    }

    /// <inheritdoc />
    public bool ShouldCreateSnapshot(AggregateRoot aggregate)
    {
        return false;
    }
}

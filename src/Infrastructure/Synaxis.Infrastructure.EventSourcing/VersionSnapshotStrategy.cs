// <copyright file="VersionSnapshotStrategy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// A snapshot strategy that creates snapshots when the aggregate reaches a specific version.
/// </summary>
public class VersionSnapshotStrategy : ISnapshotStrategy
{
    private readonly int _targetVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionSnapshotStrategy"/> class.
    /// </summary>
    /// <param name="targetVersion">The version at which to create a snapshot.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="targetVersion"/> is less than 1.</exception>
    public VersionSnapshotStrategy(int targetVersion)
    {
        if (targetVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(targetVersion), "Target version must be at least 1.");
        }

        this._targetVersion = targetVersion;
    }

    /// <inheritdoc />
    public bool ShouldCreateSnapshot(AggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        return aggregate.Version == this._targetVersion;
    }
}

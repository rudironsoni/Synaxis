// <copyright file="IntervalSnapshotStrategy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// A snapshot strategy that creates snapshots at regular version intervals.
/// </summary>
public class IntervalSnapshotStrategy : ISnapshotStrategy
{
    private readonly int _interval;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalSnapshotStrategy"/> class.
    /// </summary>
    /// <param name="interval">The version interval at which to create snapshots.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="interval"/> is less than 1.</exception>
    public IntervalSnapshotStrategy(int interval)
    {
        if (interval < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be at least 1.");
        }

        this._interval = interval;
    }

    /// <inheritdoc />
    public bool ShouldCreateSnapshot(AggregateRoot aggregate)
    {
        if (aggregate == null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        return aggregate.Version > 0 && aggregate.Version % this._interval == 0;
    }
}

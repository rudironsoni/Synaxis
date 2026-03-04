// <copyright file="IAggregateSnapshot.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing.Aggregates;

/// <summary>
/// Interface for aggregates that support snapshotting for performance optimization.
/// </summary>
public interface IAggregateSnapshot
{
    /// <summary>
    /// Restores the aggregate state from a snapshot.
    /// </summary>
    /// <param name="state">The snapshot state data.</param>
    void RestoreFromSnapshot(object state);

    /// <summary>
    /// Creates a snapshot of the current aggregate state.
    /// </summary>
    /// <returns>The snapshot state data.</returns>
    object CreateSnapshot();

    /// <summary>
    /// Gets the type of the snapshot state.
    /// </summary>
    System.Type GetSnapshotType();
}

// <copyright file="ISnapshotStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing.Persistence;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for storing and retrieving aggregate snapshots.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Gets the latest snapshot for a stream.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The snapshot state and version, or null if no snapshot exists.</returns>
    Task<SnapshotResult<TState>?> GetSnapshotAsync<TState>(
        string streamId,
        CancellationToken cancellationToken = default)
        where TState : notnull;

    /// <summary>
    /// Saves a snapshot for a stream.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="version">The version of the stream at the time of the snapshot.</param>
    /// <param name="state">The state to snapshot.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SaveSnapshotAsync<TState>(
        string streamId,
        long version,
        TState state,
        CancellationToken cancellationToken = default)
        where TState : notnull;

    /// <summary>
    /// Deletes snapshots for a stream.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DeleteSnapshotAsync(
        string streamId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a snapshot result with state and version.
/// </summary>
/// <typeparam name="TState">The type of the state.</typeparam>
public sealed class SnapshotResult<TState>
    where TState : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotResult{TState}"/> class.
    /// </summary>
    /// <param name="state">The snapshot state.</param>
    /// <param name="version">The version at the time of the snapshot.</param>
    public SnapshotResult(TState state, long version)
    {
        this.State = state;
        this.Version = version;
    }

    /// <summary>
    /// Gets the snapshot state.
    /// </summary>
    public TState State { get; }

    /// <summary>
    /// Gets the version at the time of the snapshot.
    /// </summary>
    public long Version { get; }
}

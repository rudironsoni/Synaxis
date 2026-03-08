// <copyright file="ISnapshotStrategy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing;

using Synaxis.Shared.Kernel.Application.Cloud;

/// <summary>
/// Defines a strategy for determining when to create snapshots of an aggregate.
/// </summary>
public interface ISnapshotStrategy
{
    /// <summary>
    /// Determines whether a snapshot should be created for the aggregate.
    /// </summary>
    /// <param name="aggregate">The aggregate to evaluate.</param>
    /// <returns><c>true</c> if a snapshot should be created; otherwise, <c>false</c>.</returns>
    bool ShouldCreateSnapshot(AggregateRoot aggregate);
}

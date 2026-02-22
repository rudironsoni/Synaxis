// <copyright file="ITimeProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Time;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Abstraction for time operations to enable deterministic testing.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Returns a task that completes after the specified delay.
    /// </summary>
    /// <param name="delay">The time span to delay.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the delay.</returns>
    Task Delay(TimeSpan delay, CancellationToken cancellationToken = default);
}

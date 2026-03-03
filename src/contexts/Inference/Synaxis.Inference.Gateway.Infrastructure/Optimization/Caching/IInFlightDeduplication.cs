// <copyright file="IInFlightDeduplication.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Optimization.Caching;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for in-flight request deduplication.
/// </summary>
public interface IInFlightDeduplication
{
    /// <summary>
    /// Executes an operation with deduplication to prevent duplicate concurrent executions.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="requestHash">A unique hash identifying the request.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="lockTimeout">The timeout for acquiring the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> ExecuteWithDeduplication<T>(
        string requestHash,
        Func<Task<T>> operation,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a request with the given hash is currently in flight.
    /// </summary>
    /// <param name="requestHash">A unique hash identifying the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the request is in flight, false otherwise.</returns>
    Task<bool> IsInFlightAsync(string requestHash, CancellationToken cancellationToken);
}
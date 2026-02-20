// <copyright file="IExecutionMetrics.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service for recording execution metrics and telemetry.
/// </summary>
public interface IExecutionMetrics
{
    /// <summary>
    /// Records that an execution has started.
    /// </summary>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="agentId">The agent identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordExecutionStartedAsync(
        Guid executionId,
        Guid agentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that an execution has completed.
    /// </summary>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordExecutionCompletedAsync(
        Guid executionId,
        long durationMs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that an execution has failed.
    /// </summary>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="error">The error message.</param>
    /// <param name="durationMs">The duration in milliseconds before failure.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordExecutionFailedAsync(
        Guid executionId,
        string error,
        long durationMs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that an execution has been cancelled.
    /// </summary>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="durationMs">The duration in milliseconds before cancellation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordExecutionCancelledAsync(
        Guid executionId,
        long durationMs,
        CancellationToken cancellationToken = default);
}

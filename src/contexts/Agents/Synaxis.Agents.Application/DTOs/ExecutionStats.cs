// <copyright file="ExecutionStats.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

/// <summary>
/// Execution statistics for an agent.
/// </summary>
public record ExecutionStats
{
    /// <summary>
    /// Gets total number of executions.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets number of successful executions.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Gets number of failed executions.
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// Gets average execution duration.
    /// </summary>
    public TimeSpan? AverageDuration { get; init; }

    /// <summary>
    /// Gets last execution timestamp.
    /// </summary>
    public DateTimeOffset? LastExecutedAt { get; init; }
}

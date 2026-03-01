// <copyright file="ExecutionStats.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Execution statistics for an agent.
/// </summary>
public record ExecutionStats
{
    /// <summary>
    /// Gets the total number of executions.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the number of successful executions.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("successCount")]
    public int SuccessCount { get; init; }

    /// <summary>
    /// Gets the number of failed executions.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("failureCount")]
    public int FailureCount { get; init; }

    /// <summary>
    /// Gets the average execution duration.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("averageDuration")]
    public TimeSpan? AverageDuration { get; init; }

    /// <summary>
    /// Gets the last execution timestamp.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("lastExecutedAt")]
    public DateTimeOffset? LastExecutedAt { get; init; }
}

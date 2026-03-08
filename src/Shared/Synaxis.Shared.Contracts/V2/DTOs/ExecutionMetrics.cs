// <copyright file="ExecutionMetrics.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DTOs;

/// <summary>
/// Execution metrics.
/// </summary>
public record ExecutionMetrics
{
    /// <summary>
    /// Gets the number of tokens consumed (if applicable).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tokensConsumed")]
    public int? TokensConsumed { get; init; }

    /// <summary>
    /// Gets the CPU time used.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cpuTime")]
    public TimeSpan? CpuTime { get; init; }

    /// <summary>
    /// Gets the memory used in bytes.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("memoryBytes")]
    public long? MemoryBytes { get; init; }

    /// <summary>
    /// Gets the custom metrics.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("custom")]
    public IReadOnlyDictionary<string, double>? Custom { get; init; }
}

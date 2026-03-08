// <copyright file="RetryPolicy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DomainEvents;

/// <summary>
/// Retry policy for workflow steps.
/// </summary>
public record RetryPolicy
{
    /// <summary>
    /// Gets the maximum number of retries.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("maxRetries")]
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets the initial delay between retries.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("initialDelay")]
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the backoff multiplier for retries.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("backoffMultiplier")]
    public double BackoffMultiplier { get; init; } = 2.0;
}

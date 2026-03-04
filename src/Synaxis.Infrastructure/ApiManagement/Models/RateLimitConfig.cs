// <copyright file="RateLimitConfig.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Models;

/// <summary>
/// Configuration for rate limiting in the API Management platform.
/// </summary>
public sealed record RateLimitConfig
{
    /// <summary>
    /// Gets the number of requests allowed per window.
    /// </summary>
    public required int RequestsPerWindow { get; init; }

    /// <summary>
    /// Gets the duration of the rate limit window in seconds.
    /// </summary>
    public required int WindowSeconds { get; init; }

    /// <summary>
    /// Gets the burst capacity (additional requests allowed in burst).
    /// </summary>
    public int BurstCapacity { get; init; } = 0;

    /// <summary>
    /// Gets a value indicating whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the scope of the rate limit (subscription, key, or ip).
    /// </summary>
    public RateLimitScope Scope { get; init; } = RateLimitScope.Subscription;
}

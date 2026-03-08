// <copyright file="RateLimitStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Models;

/// <summary>
/// Represents the current rate limit status for an API key.
/// </summary>
public sealed record RateLimitStatus
{
    /// <summary>
    /// Gets a value indicating whether rate limiting is currently active.
    /// </summary>
    public bool IsRateLimited { get; init; }

    /// <summary>
    /// Gets the number of requests remaining in the current window.
    /// </summary>
    public int RemainingRequests { get; init; }

    /// <summary>
    /// Gets the total number of requests allowed per window.
    /// </summary>
    public int TotalRequestsAllowed { get; init; }

    /// <summary>
    /// Gets the number of requests made in the current window.
    /// </summary>
    public int RequestsMade { get; init; }

    /// <summary>
    /// Gets the timestamp when the current window resets.
    /// </summary>
    public System.DateTimeOffset WindowResetTime { get; init; }

    /// <summary>
    /// Gets the retry after duration if rate limited.
    /// </summary>
    public System.TimeSpan? RetryAfter { get; init; }
}

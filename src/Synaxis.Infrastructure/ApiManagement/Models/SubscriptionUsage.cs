// <copyright file="SubscriptionUsage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Models;

/// <summary>
/// Represents usage statistics for a specific subscription.
/// </summary>
public sealed record SubscriptionUsage
{
    /// <summary>
    /// Gets the subscription ID.
    /// </summary>
    public required string SubscriptionId { get; init; }

    /// <summary>
    /// Gets the display name of the subscription.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the total number of calls made by this subscription.
    /// </summary>
    public long CallCount { get; init; }

    /// <summary>
    /// Gets the total data transfer in bytes.
    /// </summary>
    public long DataTransferBytes { get; init; }

    /// <summary>
    /// Gets the number of rate limit hits.
    /// </summary>
    public long RateLimitHits { get; init; }
}

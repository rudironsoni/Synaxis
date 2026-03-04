// <copyright file="RateLimitScope.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Models;

/// <summary>
/// Represents the scope of rate limiting.
/// </summary>
public enum RateLimitScope
{
    /// <summary>
    /// Rate limit applies per subscription.
    /// </summary>
    Subscription,

    /// <summary>
    /// Rate limit applies per API key.
    /// </summary>
    Key,

    /// <summary>
    /// Rate limit applies per IP address.
    /// </summary>
    Ip,

    /// <summary>
    /// Rate limit applies per user.
    /// </summary>
    User,
}

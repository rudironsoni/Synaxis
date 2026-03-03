// <copyright file="RateLimitOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Represents rate limiting options.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the requests per window.
    /// </summary>
    public int RequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Gets or sets the window size in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the burst capacity.
    /// </summary>
    public int BurstCapacity { get; set; } = 10;
}

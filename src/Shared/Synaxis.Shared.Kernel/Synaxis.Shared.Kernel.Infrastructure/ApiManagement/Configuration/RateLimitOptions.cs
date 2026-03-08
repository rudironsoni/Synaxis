// <copyright file="RateLimitOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Configuration;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Rate limiting configuration options.
/// </summary>
public sealed class RateLimitOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default requests per window.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Requests per window must be at least 1")]
    public int DefaultRequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Gets or sets the default window size in seconds.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Window seconds must be at least 1")]
    public int DefaultWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the default burst capacity.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int DefaultBurstCapacity { get; set; } = 10;
}

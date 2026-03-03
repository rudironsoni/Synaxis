// <copyright file="RetryPolicyOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Represents retry policy options.
/// </summary>
public class RetryPolicyOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether retry is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum retry count.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay in milliseconds.
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets a value indicating whether to use exponential backoff.
    /// </summary>
    public bool ExponentialBackoff { get; set; } = true;
}

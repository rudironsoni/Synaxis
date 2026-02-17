// <copyright file="CircuitBreakerOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Represents circuit breaker options.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether circuit breaker is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the failure threshold.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration of open state in seconds.
    /// </summary>
    public int DurationOfBreakSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the sampling duration in seconds.
    /// </summary>
    public int SamplingDurationSeconds { get; set; } = 60;
}

// <copyright file="HourlyUsage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Models;

using System;

/// <summary>
/// Represents hourly usage statistics.
/// </summary>
public sealed record HourlyUsage
{
    /// <summary>
    /// Gets the hour timestamp.
    /// </summary>
    public required DateTimeOffset Hour { get; init; }

    /// <summary>
    /// Gets the number of calls in this hour.
    /// </summary>
    public long CallCount { get; init; }

    /// <summary>
    /// Gets the number of successful calls.
    /// </summary>
    public long SuccessCount { get; init; }

    /// <summary>
    /// Gets the number of failed calls.
    /// </summary>
    public long ErrorCount { get; init; }

    /// <summary>
    /// Gets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; init; }
}

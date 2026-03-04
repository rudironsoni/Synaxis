// <copyright file="UsageReport.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a usage report from the API Management platform.
/// </summary>
public sealed record UsageReport
{
    /// <summary>
    /// Gets the start time of the reporting period.
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// Gets the end time of the reporting period.
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Gets the total number of API calls in the period.
    /// </summary>
    public long TotalCalls { get; init; }

    /// <summary>
    /// Gets the total number of successful calls.
    /// </summary>
    public long SuccessfulCalls { get; init; }

    /// <summary>
    /// Gets the total number of failed calls.
    /// </summary>
    public long FailedCalls { get; init; }

    /// <summary>
    /// Gets the total number of blocked calls (rate limited).
    /// </summary>
    public long BlockedCalls { get; init; }

    /// <summary>
    /// Gets the total data transfer in bytes.
    /// </summary>
    public long TotalDataTransferBytes { get; init; }

    /// <summary>
    /// Gets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the maximum response time in milliseconds.
    /// </summary>
    public double MaxResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the minimum response time in milliseconds.
    /// </summary>
    public double MinResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the detailed breakdown of calls by API endpoint.
    /// </summary>
    public IReadOnlyList<EndpointUsage> EndpointBreakdown { get; init; } = new List<EndpointUsage>();

    /// <summary>
    /// Gets the usage breakdown by subscription.
    /// </summary>
    public IReadOnlyList<SubscriptionUsage> SubscriptionBreakdown { get; init; } = new List<SubscriptionUsage>();

    /// <summary>
    /// Gets the hourly usage statistics.
    /// </summary>
    public IReadOnlyList<HourlyUsage> HourlyBreakdown { get; init; } = new List<HourlyUsage>();

    /// <summary>
    /// Gets the status code distribution.
    /// </summary>
    public IDictionary<int, long> StatusCodeDistribution { get; init; } = new Dictionary<int, long>();
}

// <copyright file="EndpointUsage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Models;

/// <summary>
/// Represents usage statistics for a specific API endpoint.
/// </summary>
public sealed record EndpointUsage
{
    /// <summary>
    /// Gets the API endpoint path.
    /// </summary>
    public required string Endpoint { get; init; }

    /// <summary>
    /// Gets the HTTP method.
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Gets the total number of calls to this endpoint.
    /// </summary>
    public long CallCount { get; init; }

    /// <summary>
    /// Gets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the number of successful calls.
    /// </summary>
    public long SuccessCount { get; init; }

    /// <summary>
    /// Gets the number of failed calls.
    /// </summary>
    public long ErrorCount { get; init; }
}

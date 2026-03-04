// <copyright file="InMemoryOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Configuration;

using System.Collections.Generic;

/// <summary>
/// Configuration options for in-memory API Management (development/testing).
/// </summary>
public sealed class InMemoryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to simulate rate limiting.
    /// </summary>
    public bool SimulateRateLimiting { get; set; } = true;

    /// <summary>
    /// Gets or sets the simulated latency in milliseconds.
    /// </summary>
    public int SimulatedLatencyMs { get; set; } = 10;

    /// <summary>
    /// Gets or sets the predefined API keys for testing.
    /// </summary>
    public IDictionary<string, string> TestKeys { get; set; } = new Dictionary<string, string>();
}

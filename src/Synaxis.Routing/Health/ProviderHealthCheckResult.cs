// <copyright file="ProviderHealthCheckResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

#nullable enable

namespace Synaxis.Routing.Health;

/// <summary>
/// Represents the result of a health check.
/// </summary>
public class ProviderHealthCheckResult
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public ProviderHealthStatus Status { get; set; }

    /// <summary>
    /// Gets a value indicating whether the health check passed.
    /// </summary>
    public bool IsHealthy => this.Status == ProviderHealthStatus.Healthy;

    /// <summary>
    /// Gets or sets the latency of the health check in milliseconds.
    /// </summary>
    public int LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the error message if the check failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional details about the health check.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0016", Justification = "Public API uses concrete type for backward compatibility")]
    public Dictionary<string, object> Details { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the timestamp when the health check was performed.
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the success rate percentage (0-100).
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider is currently in circuit breaker state.
    /// </summary>
    public bool IsCircuitBreakerOpen { get; set; }
}

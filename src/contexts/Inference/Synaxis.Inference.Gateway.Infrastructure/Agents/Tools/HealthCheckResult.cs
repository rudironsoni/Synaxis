// <copyright file="HealthCheckResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    /// <summary>
    /// Represents the result of a health check.
    /// </summary>
    /// <param name="IsHealthy">Whether the provider is healthy.</param>
    /// <param name="HealthScore">The health score.</param>
    /// <param name="ConsecutiveFailures">The number of consecutive failures.</param>
    /// <param name="IsInCooldown">Whether the provider is in cooldown.</param>
    /// <param name="CooldownUntil">The cooldown end time.</param>
    public record HealthCheckResult(bool IsHealthy, decimal HealthScore, int ConsecutiveFailures, bool IsInCooldown, DateTime? CooldownUntil);
}
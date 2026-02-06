// <copyright file="IHealthTool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    /// <summary>
    /// Tool for managing provider health status.
    /// </summary>
    public interface IHealthTool
    {
        Task<HealthCheckResult> CheckHealthAsync(Guid organizationId, Guid providerId, CancellationToken ct = default);

        Task MarkUnhealthyAsync(Guid organizationId, Guid providerId, string reason, int consecutiveFailures, CancellationToken ct = default);

        Task MarkHealthyAsync(Guid organizationId, Guid providerId, CancellationToken ct = default);

        Task ResetHealthAsync(Guid organizationId, Guid providerId, CancellationToken ct = default);
    }

    public record HealthCheckResult(bool IsHealthy, decimal HealthScore, int ConsecutiveFailures, bool IsInCooldown, DateTime? CooldownUntil);
}
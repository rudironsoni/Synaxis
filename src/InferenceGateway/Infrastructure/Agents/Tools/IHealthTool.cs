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
        /// <summary>
        /// Checks the health status of a provider.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        Task<HealthCheckResult> CheckHealthAsync(Guid organizationId, Guid providerId, CancellationToken ct = default);

        /// <summary>
        /// Marks a provider as unhealthy.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="reason">The reason for marking unhealthy.</param>
        /// <param name="consecutiveFailures">The number of consecutive failures.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task MarkUnhealthyAsync(Guid organizationId, Guid providerId, string reason, int consecutiveFailures, CancellationToken ct = default);

        /// <summary>
        /// Marks a provider as healthy.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task MarkHealthyAsync(Guid organizationId, Guid providerId, CancellationToken ct = default);

        /// <summary>
        /// Resets the health status of a provider.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ResetHealthAsync(Guid organizationId, Guid providerId, CancellationToken ct = default);
    }

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

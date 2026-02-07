// <copyright file="HealthTool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Tool for managing provider health status.
    /// </summary>
    public class HealthTool : IHealthTool
    {
        private readonly ControlPlaneDbContext _db;
        private readonly ILogger<HealthTool> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthTool"/> class.
        /// </summary>
        /// <param name="db">The database context.</param>
        /// <param name="logger">The logger.</param>
        public HealthTool(ControlPlaneDbContext db, ILogger<HealthTool> logger)
        {
            this._db = db;
            this._logger = logger;
        }

        /// <summary>
        /// Checks the health status of a provider.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(Guid organizationId, Guid providerId, CancellationToken ct = default)
        {
            try
            {
                // Query from Operations schema
                var health = await _db.Database.SqlQuery<ProviderHealthDto>(
                    $"SELECT \"IsHealthy\", \"HealthScore\", \"ConsecutiveFailures\", \"IsInCooldown\", \"CooldownUntil\" FROM operations.\"ProviderHealthStatus\" WHERE \"OrganizationProviderId\" = {providerId} ORDER BY \"LastCheckedAt\" DESC LIMIT 1").FirstOrDefaultAsync(ct);

                if (health == null)
                {
                    return new HealthCheckResult(true, 1.0m, 0, false, null);
                }

                return new HealthCheckResult(
                    health.IsHealthy,
                    health.HealthScore,
                    health.ConsecutiveFailures,
                    health.IsInCooldown,
                    health.CooldownUntil);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check health");
                return new HealthCheckResult(false, 0m, 0, false, null);
            }
        }

        /// <summary>
        /// Marks a provider as unhealthy.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="reason">The reason for marking unhealthy.</param>
        /// <param name="consecutiveFailures">The number of consecutive failures.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task MarkUnhealthyAsync(Guid organizationId, Guid providerId, string reason, int consecutiveFailures, CancellationToken ct = default)
        {
            try
            {
                // Calculate cooldown: exponential backoff, max 1 hour
                var cooldownMinutes = Math.Min(Math.Pow(2, consecutiveFailures - 1), 60);
                var cooldownUntil = DateTime.UtcNow.AddMinutes(cooldownMinutes);

                await _db.Database.ExecuteSqlAsync(
                    $"UPDATE operations.\"ProviderHealthStatus\" SET \"IsHealthy\" = false, \"ConsecutiveFailures\" = {consecutiveFailures}, \"LastErrorMessage\" = {reason}, \"IsInCooldown\" = true, \"CooldownUntil\" = {cooldownUntil}, \"LastCheckedAt\" = {DateTime.UtcNow} WHERE \"OrganizationProviderId\" = {providerId}",
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark unhealthy");
            }
        }

        /// <summary>
        /// Marks a provider as healthy.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task MarkHealthyAsync(Guid organizationId, Guid providerId, CancellationToken ct = default)
        {
            try
            {
                await _db.Database.ExecuteSqlAsync(
                    $"UPDATE operations.\"ProviderHealthStatus\" SET \"IsHealthy\" = true, \"ConsecutiveFailures\" = 0, \"IsInCooldown\" = false, \"CooldownUntil\" = NULL, \"LastSuccessAt\" = {DateTime.UtcNow}, \"LastCheckedAt\" = {DateTime.UtcNow} WHERE \"OrganizationProviderId\" = {providerId}",
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark healthy");
            }
        }

        /// <summary>
        /// Resets the health status of a provider.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ResetHealthAsync(Guid organizationId, Guid providerId, CancellationToken ct = default)
        {
            try
            {
                await _db.Database.ExecuteSqlAsync(
                    $"UPDATE operations.\"ProviderHealthStatus\" SET \"IsHealthy\" = true, \"HealthScore\" = 1.0, \"ConsecutiveFailures\" = 0, \"IsInCooldown\" = false, \"CooldownUntil\" = NULL, \"LastCheckedAt\" = {DateTime.UtcNow} WHERE \"OrganizationProviderId\" = {providerId}",
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset health");
            }
        }

        private sealed class ProviderHealthDto
        {
            public bool IsHealthy { get; set; }

            public decimal HealthScore { get; set; }

            public int ConsecutiveFailures { get; set; }

            public bool IsInCooldown { get; set; }

            public DateTime? CooldownUntil { get; set; }
        }
    }
}

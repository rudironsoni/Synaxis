// <copyright file="HealthTool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    public class HealthTool : IHealthTool
    {
        private readonly ControlPlaneDbContext _db;
        private readonly ILogger<HealthTool> _logger;

        public HealthTool(ControlPlaneDbContext db, ILogger<HealthTool> logger)
        {
            _db = db;
            _logger = logger;
        }

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
// <copyright file="HealthMonitoringAgent.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Jobs
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Quartz;
    using Synaxis.InferenceGateway.Infrastructure.Agents.Tools;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Health Monitoring Agent - Checks provider health every 2 minutes.
    /// Updates ProviderHealthStatus and sends alerts for unhealthy providers.
    /// </summary>
    [DisallowConcurrentExecution]
    public class HealthMonitoringAgent : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HealthMonitoringAgent> _logger;

        public HealthMonitoringAgent(IServiceProvider serviceProvider, ILogger<HealthMonitoringAgent> logger)
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var correlationId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogInformation("[HealthMonitoring][{CorrelationId}] Starting health check", correlationId);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var healthTool = scope.ServiceProvider.GetRequiredService<IHealthTool>();
            var alertTool = scope.ServiceProvider.GetRequiredService<IAlertTool>();
            var auditTool = scope.ServiceProvider.GetRequiredService<IAuditTool>();

            try
            {
                // Get all organizations and their enabled providers
                var orgProviders = await db.Database.SqlQuery<OrgProviderDto>(
                    $"SELECT \"Id\", \"OrganizationId\", \"IsEnabled\", \"HealthCheckEnabled\" FROM operations.\"OrganizationProviders\" WHERE \"IsEnabled\" = true AND \"HealthCheckEnabled\" = true"
                ).ToListAsync(context.CancellationToken);

                _logger.LogInformation("[HealthMonitoring][{CorrelationId}] Checking {Count} providers", correlationId, orgProviders.Count);

                int checkedCount = 0;
                int unhealthyCount = 0;

                foreach (var provider in orgProviders)
                {
                    try
                    {
                        // Check current health status
                        var health = await healthTool.CheckHealthAsync(provider.OrganizationId, provider.Id, context.CancellationToken);

                        // Skip if in cooldown
                        if (health.IsInCooldown && health.CooldownUntil > DateTime.UtcNow)
                        {
                            _logger.LogDebug("[HealthMonitoring][{CorrelationId}] Provider {ProviderId} in cooldown until {Until}",
                                correlationId, provider.Id, health.CooldownUntil);
                            continue;
                        }

                        // Perform actual health check (simplified - just check if provider exists)
                        // In real implementation, this would make test API call to provider
                        bool isHealthy = await PerformHealthCheckAsync(provider.Id, context.CancellationToken);

                        if (!isHealthy)
                        {
                            int consecutiveFailures = health.ConsecutiveFailures + 1;
                            await healthTool.MarkUnhealthyAsync(
                                provider.OrganizationId,
                                provider.Id,
                                "Health check failed",
                                consecutiveFailures,
                                context.CancellationToken);

                            unhealthyCount++;

                            // Send alert on first failure or every 5th failure
                            if (consecutiveFailures == 1 || consecutiveFailures % 5 == 0)
                            {
                                await alertTool.SendAdminAlertAsync(
                                    "Provider Health Alert",
                                    $"Provider {provider.Id} has failed {consecutiveFailures} consecutive health checks",
                                    consecutiveFailures >= 5 ? AlertSeverity.Critical : AlertSeverity.Warning,
                                    context.CancellationToken);
                            }

                            await auditTool.LogActionAsync(
                                "HealthMonitoring",
                                "ProviderUnhealthy",
                                provider.OrganizationId,
                                null,
                                $"Provider {provider.Id} marked unhealthy, consecutive failures: {consecutiveFailures}",
                                correlationId,
                                context.CancellationToken);
                        }
                        else if (!health.IsHealthy)
                        {
                            // Provider recovered
                            await healthTool.MarkHealthyAsync(provider.OrganizationId, provider.Id, context.CancellationToken);

                            await alertTool.SendAdminAlertAsync(
                                "Provider Recovered",
                                $"Provider {provider.Id} is now healthy after {health.ConsecutiveFailures} failures",
                                AlertSeverity.Info,
                                context.CancellationToken);

                            await auditTool.LogActionAsync(
                                "HealthMonitoring",
                                "ProviderRecovered",
                                provider.OrganizationId,
                                null,
                                $"Provider {provider.Id} recovered after {health.ConsecutiveFailures} failures",
                                correlationId,
                                context.CancellationToken);
                        }

                        checkedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[HealthMonitoring][{CorrelationId}] Error checking provider {ProviderId}",
                            correlationId, provider.Id);
                    }
                }

                _logger.LogInformation(
                    "[HealthMonitoring][{CorrelationId}] Completed: Checked={Checked}, Unhealthy={Unhealthy}",
                    correlationId, checkedCount, unhealthyCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HealthMonitoring][{CorrelationId}] Job failed", correlationId);
            }
        }

        private async Task<bool> PerformHealthCheckAsync(Guid providerId, CancellationToken ct)
        {
            // NOTE: Implement actual health check
            // - Make test API call to provider
            // - Check response time
            // - Verify authentication
            await Task.Delay(10, ct); // Simulate check
            return true; // For now, assume healthy
        }

        private class OrgProviderDto
        {
            public Guid Id { get; set; }
            public Guid OrganizationId { get; set; }
            public bool IsEnabled { get; set; }
            public bool HealthCheckEnabled { get; set; }
        }
    }
}

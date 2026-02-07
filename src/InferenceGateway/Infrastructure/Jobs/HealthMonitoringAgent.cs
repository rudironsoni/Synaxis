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

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthMonitoringAgent"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        public HealthMonitoringAgent(IServiceProvider serviceProvider, ILogger<HealthMonitoringAgent> logger)
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
        }

        /// <inheritdoc/>
        public async Task Execute(IJobExecutionContext context)
        {
            var correlationId = Guid.NewGuid().ToString("N")[..8];
            this._logger.LogInformation("[HealthMonitoring][{CorrelationId}] Starting health check", correlationId);

            using var scope = this._serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var healthTool = scope.ServiceProvider.GetRequiredService<IHealthTool>();
            var alertTool = scope.ServiceProvider.GetRequiredService<IAlertTool>();
            var auditTool = scope.ServiceProvider.GetRequiredService<IAuditTool>();

            try
            {
                var orgProviders = await GetEnabledProvidersAsync(db, context.CancellationToken).ConfigureAwait(false);

                this._logger.LogInformation("[HealthMonitoring][{CorrelationId}] Checking {Count} providers", correlationId, orgProviders.Count);

                int checkedCount = 0;
                int unhealthyCount = 0;

                foreach (var provider in orgProviders)
                {
                    try
                    {
                        var result = await this.CheckProviderHealthAsync(provider, healthTool, alertTool, auditTool, correlationId, context.CancellationToken).ConfigureAwait(false);
                        if (result.Checked)
                        {
                            checkedCount++;
                        }

                        if (result.Unhealthy)
                        {
                            unhealthyCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError(
                            ex,
                            "[HealthMonitoring][{CorrelationId}] Error checking provider {ProviderId}",
                            correlationId,
                            provider.Id);
                    }
                }

                this._logger.LogInformation(
                    "[HealthMonitoring][{CorrelationId}] Completed: Checked={Checked}, Unhealthy={Unhealthy}",
                    correlationId,
                    checkedCount,
                    unhealthyCount);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "[HealthMonitoring][{CorrelationId}] Job failed", correlationId);
            }
        }

        private static Task<List<OrgProviderDto>> GetEnabledProvidersAsync(ControlPlaneDbContext db, CancellationToken ct)
        {
            return db.Database.SqlQuery<OrgProviderDto>(
                $"SELECT \"Id\", \"OrganizationId\", \"IsEnabled\", \"HealthCheckEnabled\" FROM operations.\"OrganizationProviders\" WHERE \"IsEnabled\" = true AND \"HealthCheckEnabled\" = true").ToListAsync(ct);
        }

        private async Task<CheckResult> CheckProviderHealthAsync(
            OrgProviderDto provider,
            IHealthTool healthTool,
            IAlertTool alertTool,
            IAuditTool auditTool,
            string correlationId,
            CancellationToken ct)
        {
            var health = await healthTool.CheckHealthAsync(provider.OrganizationId, provider.Id, ct).ConfigureAwait(false);

            if (health.isInCooldown && health.cooldownUntil > DateTime.UtcNow)
            {
                this._logger.LogDebug(
                    "[HealthMonitoring][{CorrelationId}] Provider {ProviderId} in cooldown until {Until}",
                    correlationId,
                    provider.Id,
                    health.cooldownUntil);
                return new CheckResult { Checked = false, Unhealthy = false };
            }

            bool isHealthy = await PerformHealthCheckAsync(ct).ConfigureAwait(false);

            if (!isHealthy)
            {
                await HandleUnhealthyProviderAsync(provider, health, healthTool, alertTool, auditTool, correlationId, ct).ConfigureAwait(false);
                return new CheckResult { Checked = true, Unhealthy = true };
            }

            if (!health.isHealthy)
            {
                await HandleProviderRecoveryAsync(provider, health, healthTool, alertTool, auditTool, correlationId, ct).ConfigureAwait(false);
            }

            return new CheckResult { Checked = true, Unhealthy = false };
        }

        private static async Task HandleUnhealthyProviderAsync(
            OrgProviderDto provider,
            Agents.Tools.HealthCheckResult health,
            IHealthTool healthTool,
            IAlertTool alertTool,
            IAuditTool auditTool,
            string correlationId,
            CancellationToken ct)
        {
            int consecutiveFailures = health.consecutiveFailures + 1;
            await healthTool.MarkUnhealthyAsync(
                provider.OrganizationId,
                provider.Id,
                "Health check failed",
                consecutiveFailures,
                ct).ConfigureAwait(false);

            if (consecutiveFailures == 1 || consecutiveFailures % 5 == 0)
            {
                await alertTool.SendAdminAlertAsync(
                    "Provider Health Alert",
                    $"Provider {provider.Id} has failed {consecutiveFailures} consecutive health checks",
                    consecutiveFailures >= 5 ? AlertSeverity.Critical : AlertSeverity.Warning,
                    ct).ConfigureAwait(false);
            }

            await auditTool.LogActionAsync(
                "HealthMonitoring",
                "ProviderUnhealthy",
                provider.OrganizationId,
                null,
                $"Provider {provider.Id} marked unhealthy, consecutive failures: {consecutiveFailures}",
                correlationId,
                ct).ConfigureAwait(false);
        }

        private static async Task HandleProviderRecoveryAsync(
            OrgProviderDto provider,
            Agents.Tools.HealthCheckResult health,
            IHealthTool healthTool,
            IAlertTool alertTool,
            IAuditTool auditTool,
            string correlationId,
            CancellationToken ct)
        {
            await healthTool.MarkHealthyAsync(provider.OrganizationId, provider.Id, ct).ConfigureAwait(false);

            await alertTool.SendAdminAlertAsync(
                "Provider Recovered",
                $"Provider {provider.Id} is now healthy after {health.consecutiveFailures} failures",
                AlertSeverity.Info,
                ct).ConfigureAwait(false);

            await auditTool.LogActionAsync(
                "HealthMonitoring",
                "ProviderRecovered",
                provider.OrganizationId,
                null,
                $"Provider {provider.Id} recovered after {health.consecutiveFailures} failures",
                correlationId,
                ct).ConfigureAwait(false);
        }

        private static async Task<bool> PerformHealthCheckAsync(CancellationToken ct)
        {
            // NOTE: Implement actual health check
            // - Make test API call to provider
            // - Check response time
            // - Verify authentication
            await Task.Delay(10, ct).ConfigureAwait(false); // Simulate check
            return true; // For now, assume healthy
        }

        private sealed class CheckResult
        {
            /// <summary>
            /// Gets or sets a value indicating whether the check was performed.
            /// </summary>
            public bool Checked { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the provider is unhealthy.
            /// </summary>
            public bool Unhealthy { get; set; }
        }

        private sealed class OrgProviderDto
        {
            /// <summary>
            /// Gets or sets the Id.
            /// </summary>
            public Guid Id { get; set; }

            /// <summary>
            /// Gets or sets the OrganizationId.
            /// </summary>
            public Guid OrganizationId { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the provider is enabled.
            /// </summary>
            public bool IsEnabled { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether health check is enabled.
            /// </summary>
            public bool HealthCheckEnabled { get; set; }
        }
    }
}

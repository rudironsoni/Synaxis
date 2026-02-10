// <copyright file="SecurityAuditAgent.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Jobs
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Quartz;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Infrastructure.Agents.Tools;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Security Audit Agent - Runs every 6 hours.
    /// Audits security configuration and access patterns.
    /// </summary>
    [DisallowConcurrentExecution]
    public class SecurityAuditAgent : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SecurityAuditAgent> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityAuditAgent"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        /// <param name="logger">The logger instance for logging security audit operations.</param>
        public SecurityAuditAgent(IServiceProvider serviceProvider, ILogger<SecurityAuditAgent> logger)
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
        }

        /// <inheritdoc/>
        public async Task Execute(IJobExecutionContext context)
        {
            var correlationId = Guid.NewGuid().ToString("N")[..8];
            this._logger.LogInformation("[SecurityAudit][{CorrelationId}] Starting security audit", correlationId);

            using var scope = this._serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var alertTool = scope.ServiceProvider.GetRequiredService<IAlertTool>();
            var auditTool = scope.ServiceProvider.GetRequiredService<IAuditTool>();

            var issues = new List<SecurityIssue>();

            try
            {
                await this.CheckJwtSecretAsync(config, issues).ConfigureAwait(false);
                await this.CheckInactiveApiKeysAsync(db, issues, context.CancellationToken).ConfigureAwait(false);
                await this.CheckFailedLoginsAsync(db, issues, context.CancellationToken).ConfigureAwait(false);
                await this.CheckMissingRateLimitsAsync(db, issues, context.CancellationToken).ConfigureAwait(false);
                await this.CheckUnusualAccessPatternsAsync(db, issues, context.CancellationToken).ConfigureAwait(false);
                await this.CheckExcessivePermissionsAsync(db, issues, context.CancellationToken).ConfigureAwait(false);

                await this.ReportAuditResultsAsync(issues, alertTool, auditTool, correlationId, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "[SecurityAudit][{CorrelationId}] Job failed", correlationId);
            }
        }

        private async Task CheckJwtSecretAsync(IConfiguration config, List<SecurityIssue> issues)
        {
            var jwtSecret = config["Synaxis:InferenceGateway:JwtSecret"];
            if (string.IsNullOrEmpty(jwtSecret))
            {
                return;
            }

            if (jwtSecret.Length < 32)
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Critical,
                    Category = "Configuration",
                    Description = "JWT secret is too short (< 32 characters)",
                    Recommendation = "Use a longer, cryptographically random secret",
                });
            }

            if (jwtSecret.Contains("default", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Critical,
                    Category = "Configuration",
                    Description = "JWT secret appears to be a default or weak value",
                    Recommendation = "Generate a strong, unique JWT secret",
                });
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task CheckInactiveApiKeysAsync(ControlPlaneDbContext db, List<SecurityIssue> issues, CancellationToken ct)
        {
            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
            var inactiveKeys = await db.ApiKeys
                .Where(k => k.LastUsedAt < ninetyDaysAgo && k.Status == ApiKeyStatus.Active)
                .CountAsync(ct).ConfigureAwait(false);

            if (inactiveKeys > 0)
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Warning,
                    Category = "AccessControl",
                    Description = $"{inactiveKeys} API keys have not been used in 90+ days",
                    Recommendation = "Review and revoke unused API keys",
                });
            }
        }

        private async Task CheckFailedLoginsAsync(ControlPlaneDbContext db, List<SecurityIssue> issues, CancellationToken ct)
        {
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var failedLogins = await db.AuditLogs
                .Where(a => a.Action.Contains("LoginFailed") && a.CreatedAt >= oneDayAgo)
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .Where(x => x.Count >= 5)
                .ToListAsync(ct).ConfigureAwait(false);

            if (failedLogins.Any())
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Warning,
                    Category = "AccessControl",
                    Description = $"{failedLogins.Count} users with 5+ failed login attempts in 24h",
                    Recommendation = "Review suspicious login activity and consider implementing rate limiting",
                });
            }
        }

        private async Task CheckMissingRateLimitsAsync(ControlPlaneDbContext db, List<SecurityIssue> issues, CancellationToken ct)
        {
            var providersWithoutLimits = await db.Database.SqlQuery<ProviderDto>(
                $"SELECT \"Id\" FROM operations.\"OrganizationProvider\" WHERE \"IsEnabled\" = true AND (\"RateLimitRpm\" IS NULL OR \"RateLimitTpm\" IS NULL)").CountAsync(ct).ConfigureAwait(false);

            if (providersWithoutLimits > 0)
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Info,
                    Category = "Configuration",
                    Description = $"{providersWithoutLimits} providers without rate limits configured",
                    Recommendation = "Configure rate limits for all providers",
                });
            }
        }

        private async Task CheckUnusualAccessPatternsAsync(ControlPlaneDbContext db, List<SecurityIssue> issues, CancellationToken ct)
        {
            var oneHourAgo = new DateTimeOffset(DateTime.UtcNow.AddHours(-1));
            var highVolumeUsers = await db.RequestLogs
                .Where(r => r.CreatedAt >= oneHourAgo && r.UserId != null)
                .GroupBy(r => r.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .Where(x => x.Count >= 1000)
                .ToListAsync(ct).ConfigureAwait(false);

            if (highVolumeUsers.Any())
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Info,
                    Category = "Usage",
                    Description = $"{highVolumeUsers.Count} users with 1000+ requests in last hour",
                    Recommendation = "Review for potential abuse or misconfiguration",
                });
            }
        }

        private async Task CheckExcessivePermissionsAsync(ControlPlaneDbContext db, List<SecurityIssue> issues, CancellationToken ct)
        {
            var adminCount = await db.Users
                .Where(u => u.Role.ToString() == "Admin")
                .CountAsync(ct).ConfigureAwait(false);

            if (adminCount > 5)
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Info,
                    Category = "AccessControl",
                    Description = $"{adminCount} users with admin privileges",
                    Recommendation = "Follow principle of least privilege",
                });
            }
        }

        private async Task ReportAuditResultsAsync(
            List<SecurityIssue> issues,
            IAlertTool alertTool,
            IAuditTool auditTool,
            string correlationId,
            CancellationToken ct)
        {
            this._logger.LogInformation(
                "[SecurityAudit][{CorrelationId}] Completed: Found {Count} issues ({Critical} critical, {Warning} warnings)",
                correlationId,
                issues.Count,
                issues.Count(i => i.Severity == AlertSeverity.Critical),
                issues.Count(i => i.Severity == AlertSeverity.Warning));

            var criticalIssues = issues.Where(i => i.Severity == AlertSeverity.Critical).ToList();
            if (criticalIssues.Any())
            {
                var report = string.Join("\n", criticalIssues.Select(i => $"- {i.Category}: {i.Description}"));
                await alertTool.SendAdminAlertAsync(
                    "Critical Security Issues Detected",
                    $"Security audit found {criticalIssues.Count} critical issues:\n{report}",
                    AlertSeverity.Critical,
                    ct).ConfigureAwait(false);
            }

            await auditTool.LogActionAsync(
                "SecurityAudit",
                "AuditCompleted",
                null,
                null,
                $"Found {issues.Count} issues: {issues.Count(i => i.Severity == AlertSeverity.Critical)} critical, {issues.Count(i => i.Severity == AlertSeverity.Warning)} warnings, {issues.Count(i => i.Severity == AlertSeverity.Info)} info",
                correlationId,
                ct).ConfigureAwait(false);

            foreach (var issue in issues)
            {
                this._logger.LogWarning(
                    "[SecurityAudit][{CorrelationId}][{Severity}] {Category}: {Description} - {Recommendation}",
                    correlationId,
                    issue.Severity,
                    issue.Category,
                    issue.Description,
                    issue.Recommendation);
            }
        }

        private sealed class SecurityIssue
        {
            /// <summary>
            /// Gets or sets the Severity.
            /// </summary>
            public AlertSeverity Severity { get; set; }

            /// <summary>
            /// Gets or sets the Category.
            /// </summary>
            public string Category { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the Description.
            /// </summary>
            public string Description { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the Recommendation.
            /// </summary>
            public string Recommendation { get; set; } = string.Empty;
        }

        private sealed class ProviderDto
        {
            /// <summary>
            /// Gets or sets the Id.
            /// </summary>
            public Guid Id { get; set; }
        }
    }
}

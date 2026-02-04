using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Synaxis.InferenceGateway.Infrastructure.Agents.Tools;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

namespace Synaxis.InferenceGateway.Infrastructure.Jobs;

/// <summary>
/// Security Audit Agent - Runs every 6 hours.
/// Audits security configuration and access patterns.
/// </summary>
[DisallowConcurrentExecution]
public class SecurityAuditAgent : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SecurityAuditAgent> _logger;

    public SecurityAuditAgent(IServiceProvider serviceProvider, ILogger<SecurityAuditAgent> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("[SecurityAudit][{CorrelationId}] Starting security audit", correlationId);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var alertTool = scope.ServiceProvider.GetRequiredService<IAlertTool>();
        var auditTool = scope.ServiceProvider.GetRequiredService<IAuditTool>();

        var issues = new List<SecurityIssue>();

        try
        {
            // 1. Check JWT secret strength
            var jwtSecret = config["Synaxis:InferenceGateway:JwtSecret"];
            if (!string.IsNullOrEmpty(jwtSecret))
            {
                if (jwtSecret.Length < 32)
                {
                    issues.Add(new SecurityIssue
                    {
                        Severity = AlertSeverity.Critical,
                        Category = "Configuration",
                        Description = "JWT secret is too short (< 32 characters)",
                        Recommendation = "Use a longer, cryptographically random secret"
                    });
                }

                if (jwtSecret.Contains("default", StringComparison.OrdinalIgnoreCase) ||
                    jwtSecret.Contains("secret", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new SecurityIssue
                    {
                        Severity = AlertSeverity.Critical,
                        Category = "Configuration",
                        Description = "JWT secret appears to be a default or weak value",
                        Recommendation = "Generate a strong, unique JWT secret"
                    });
                }
            }

            // 2. Check for inactive API keys (>90 days)
            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
            var inactiveKeys = await db.ApiKeys
                .Where(k => k.LastUsedAt < ninetyDaysAgo && k.IsActive)
                .CountAsync(context.CancellationToken);

            if (inactiveKeys > 0)
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Warning,
                    Category = "AccessControl",
                    Description = $"{inactiveKeys} API keys have not been used in 90+ days",
                    Recommendation = "Review and revoke unused API keys"
                });
            }

            // 3. Check for failed login attempts (last 24 hours)
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var failedLogins = await db.AuditLogs
                .Where(a => a.Action.Contains("LoginFailed") && a.CreatedAt >= oneDayAgo)
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .Where(x => x.Count >= 5)
                .ToListAsync(context.CancellationToken);

            if (failedLogins.Any())
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Warning,
                    Category = "AccessControl",
                    Description = $"{failedLogins.Count} users with 5+ failed login attempts in 24h",
                    Recommendation = "Review suspicious login activity and consider implementing rate limiting"
                });
            }

            // 4. Check for missing rate limits
            var providersWithoutLimits = await db.Database.SqlQuery<ProviderDto>(
                $"SELECT \"Id\" FROM operations.\"OrganizationProvider\" WHERE \"IsEnabled\" = true AND (\"RateLimitRpm\" IS NULL OR \"RateLimitTpm\" IS NULL)"
            ).CountAsync(context.CancellationToken);

            if (providersWithoutLimits > 0)
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Info,
                    Category = "Configuration",
                    Description = $"{providersWithoutLimits} providers without rate limits configured",
                    Recommendation = "Configure rate limits for all providers"
                });
            }

            // 5. Check for unusual access patterns (high-volume users in last hour)
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var highVolumeUsers = await db.RequestLogs
                .Where(r => r.CreatedAt >= oneHourAgo && r.UserId != null)
                .GroupBy(r => r.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .Where(x => x.Count >= 1000)
                .ToListAsync(context.CancellationToken);

            if (highVolumeUsers.Any())
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Info,
                    Category = "Usage",
                    Description = $"{highVolumeUsers.Count} users with 1000+ requests in last hour",
                    Recommendation = "Review for potential abuse or misconfiguration"
                });
            }

            // 6. Check for excessive permissions (users with admin role)
            var adminCount = await db.Users
                .Where(u => u.Role.ToString() == "Admin")
                .CountAsync(context.CancellationToken);

            if (adminCount > 5)
            {
                issues.Add(new SecurityIssue
                {
                    Severity = AlertSeverity.Info,
                    Category = "AccessControl",
                    Description = $"{adminCount} users with admin privileges",
                    Recommendation = "Follow principle of least privilege"
                });
            }

            // Generate audit report
            _logger.LogInformation(
                "[SecurityAudit][{CorrelationId}] Completed: Found {Count} issues ({Critical} critical, {Warning} warnings)",
                correlationId, 
                issues.Count,
                issues.Count(i => i.Severity == AlertSeverity.Critical),
                issues.Count(i => i.Severity == AlertSeverity.Warning));

            // Send alerts for critical issues
            var criticalIssues = issues.Where(i => i.Severity == AlertSeverity.Critical).ToList();
            if (criticalIssues.Any())
            {
                var report = string.Join("\n", criticalIssues.Select(i => $"- {i.Category}: {i.Description}"));
                await alertTool.SendAdminAlertAsync(
                    "Critical Security Issues Detected",
                    $"Security audit found {criticalIssues.Count} critical issues:\n{report}",
                    AlertSeverity.Critical,
                    context.CancellationToken);
            }

            // Log audit summary
            await auditTool.LogActionAsync(
                "SecurityAudit",
                "AuditCompleted",
                null,
                null,
                $"Found {issues.Count} issues: {issues.Count(i => i.Severity == AlertSeverity.Critical)} critical, {issues.Count(i => i.Severity == AlertSeverity.Warning)} warnings, {issues.Count(i => i.Severity == AlertSeverity.Info)} info",
                correlationId,
                context.CancellationToken);

            // Log all issues to audit log
            foreach (var issue in issues)
            {
                _logger.LogWarning(
                    "[SecurityAudit][{CorrelationId}][{Severity}] {Category}: {Description} - {Recommendation}",
                    correlationId, issue.Severity, issue.Category, issue.Description, issue.Recommendation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SecurityAudit][{CorrelationId}] Job failed", correlationId);
        }
    }

    private class SecurityIssue
    {
        public AlertSeverity Severity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    private class ProviderDto
    {
        public Guid Id { get; set; }
    }
}

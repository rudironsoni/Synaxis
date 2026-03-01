// <copyright file="AuditAlertService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventHandlers.Audit;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;

/// <summary>
/// Service for evaluating audit logs and generating security alerts.
/// Implements rule-based alert evaluation for suspicious activity detection.
/// </summary>
public class AuditAlertService : IAuditAlertService
{
    private readonly IAuditLogRepository _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<AuditAlertService> _logger;

    // In-memory storage for active alerts (in production, use distributed cache/database)
    private readonly ConcurrentDictionary<Guid, List<AuditAlertEvent>> _activeAlerts = new();

    // Alert rule configuration
    private const int FailedLoginThreshold = 5;
    private const int FailedLoginWindowMinutes = 15;
    private const int UnusualAccessThreshold = 10;
    private const int UnusualAccessWindowMinutes = 5;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditAlertService"/> class.
    /// </summary>
    /// <param name="repository">The audit log repository.</param>
    /// <param name="mediator">The mediator for publishing events.</param>
    /// <param name="logger">The logger.</param>
    public AuditAlertService(
        IAuditLogRepository repository,
        IMediator mediator,
        ILogger<AuditAlertService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);
        this._repository = repository;
        this._mediator = mediator;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task EvaluateForAlertsAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        // Evaluate for brute force login attempts
        await this.EvaluateBruteForceLoginAsync(auditLog, cancellationToken).ConfigureAwait(false);

        // Evaluate for unusual access patterns
        await this.EvaluateUnusualAccessAsync(auditLog, cancellationToken).ConfigureAwait(false);

        // Evaluate for data exfiltration patterns
        await this.EvaluateDataExfiltrationAsync(auditLog, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<AuditAlertEvent>> GetActiveAlertsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        }

        if (this._activeAlerts.TryGetValue(organizationId, out var alerts))
        {
            // Filter out alerts older than 24 hours
            var activeAlerts = alerts.Where(a => a.OccurredOn > DateTime.UtcNow.AddHours(-24));
            return Task.FromResult(activeAlerts);
        }

        return Task.FromResult(Enumerable.Empty<AuditAlertEvent>());
    }

    /// <summary>
    /// Evaluates for brute force login attempts.
    /// </summary>
    private async Task EvaluateBruteForceLoginAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        // Only check failed login events
        if (!this.IsFailedLoginEvent(auditLog))
        {
            return;
        }

        var windowStart = DateTime.UtcNow.AddMinutes(-FailedLoginWindowMinutes);
        var criteria = new AuditSearchCriteria(
            OrganizationId: auditLog.OrganizationId,
            UserId: auditLog.UserId,
            EventType: "auth.login_failed",
            FromDate: windowStart,
            PageSize: 100);

        var result = await this._repository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);
        var recentFailedLogins = result.Items.Count;

        if (recentFailedLogins >= FailedLoginThreshold)
        {
            var alert = new AuditAlertEvent
            {
                OrganizationId = auditLog.OrganizationId,
                UserId = auditLog.UserId,
                AlertType = "BruteForceLogin",
                Severity = "High",
                Message = $"Detected {recentFailedLogins} failed login attempts within {FailedLoginWindowMinutes} minutes",
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["failedLoginCount"] = recentFailedLogins,
                    ["windowMinutes"] = FailedLoginWindowMinutes,
                    ["ipAddress"] = auditLog.IpAddress ?? "unknown",
                },
            };

            await this.RaiseAlertAsync(alert, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Evaluates for unusual access patterns.
    /// </summary>
    private async Task EvaluateUnusualAccessAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        // Check for rapid sequential access to different resources
        var windowStart = DateTime.UtcNow.AddMinutes(-UnusualAccessWindowMinutes);
        var criteria = new AuditSearchCriteria(
            OrganizationId: auditLog.OrganizationId,
            UserId: auditLog.UserId,
            FromDate: windowStart,
            PageSize: 100);

        var result = await this._repository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);
        var recentEvents = result.Items.ToList();

        // Check for high-frequency access
        if (recentEvents.Count >= UnusualAccessThreshold)
        {
            var distinctResources = recentEvents
                .Select(e => e.ResourceId)
                .Distinct(StringComparer.Ordinal)
                .Count();

            var alert = new AuditAlertEvent
            {
                OrganizationId = auditLog.OrganizationId,
                UserId = auditLog.UserId,
                AlertType = "UnusualAccess",
                Severity = "Medium",
                Message = $"Detected {recentEvents.Count} events accessing {distinctResources} distinct resources within {UnusualAccessWindowMinutes} minutes",
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["eventCount"] = recentEvents.Count,
                    ["distinctResources"] = distinctResources,
                    ["windowMinutes"] = UnusualAccessWindowMinutes,
                },
            };

            await this.RaiseAlertAsync(alert, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Evaluates for data exfiltration patterns.
    /// </summary>
    private async Task EvaluateDataExfiltrationAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        // Check for bulk export operations
        if (IsBulkExportEvent(auditLog))
        {
            var alert = new AuditAlertEvent
            {
                OrganizationId = auditLog.OrganizationId,
                UserId = auditLog.UserId,
                AlertType = "DataExfiltration",
                Severity = "High",
                Message = "Detected bulk data export operation",
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["eventType"] = auditLog.EventType,
                    ["action"] = auditLog.Action,
                    ["resourceType"] = auditLog.ResourceType,
                    ["ipAddress"] = auditLog.IpAddress ?? "unknown",
                },
            };

            await this.RaiseAlertAsync(alert, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Raises an alert by storing it and publishing the event.
    /// </summary>
    private Task RaiseAlertAsync(AuditAlertEvent alert, CancellationToken cancellationToken)
    {
        this._logger.LogWarning(
            "Security alert raised: {AlertType} for organization {OrganizationId} - {Message}",
            alert.AlertType,
            alert.OrganizationId,
            alert.Message);

        // Store the alert
        this._activeAlerts.AddOrUpdate(
            alert.OrganizationId,
            [alert],
            (_, alerts) =>
            {
                alerts.Add(alert);
                return alerts;
            });

        // Publish the alert event
        return this._mediator.Publish(alert, cancellationToken);
    }

    /// <summary>
    /// Checks if the audit log represents a failed login event.
    /// </summary>
    private bool IsFailedLoginEvent(AuditLog log)
    {
        return string.Equals(log.EventType, "auth.login_failed", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(log.EventType, "login_failed", StringComparison.OrdinalIgnoreCase) ||
               (string.Equals(log.EventCategory, "authentication", StringComparison.OrdinalIgnoreCase) &&
                log.Action.Contains("failed", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the audit log represents a bulk export operation.
    /// </summary>
    private static bool IsBulkExportEvent(AuditLog log)
    {
        return string.Equals(log.Action, "export", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(log.Action, "bulk_export", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(log.EventType, "data.export", StringComparison.OrdinalIgnoreCase);
    }
}

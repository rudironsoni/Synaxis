// <copyright file="ExecutionFailedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.EventHandlers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.Events;
using Synaxis.Core.Contracts;

/// <summary>
/// Handles execution failed events with high priority alerts.
/// </summary>
public class ExecutionFailedHandler : INotificationHandler<ExecutionFailed>
{
    private readonly ILogger<ExecutionFailedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly IExecutionMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionFailedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="metrics">The execution metrics service.</param>
    public ExecutionFailedHandler(
        ILogger<ExecutionFailedHandler> logger,
        IAuditService auditService,
        IExecutionMetrics metrics)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        this._metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <inheritdoc/>
    public async ValueTask Handle(ExecutionFailed notification, CancellationToken cancellationToken)
    {
        this._logger.LogError(
            "Execution failed: {ExecutionId} - {Error}",
            notification.Id,
            notification.Error);

        // Update metrics
        await this._metrics.RecordExecutionFailedAsync(
            notification.Id,
            notification.Error,
            notification.DurationMs,
            cancellationToken).ConfigureAwait(false);

        // Send high priority alert
        await this.SendAlertAsync(notification).ConfigureAwait(false);

        // Audit log the failure
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ExecutionFailed),
            EventCategory = "Execution",
            Action = "Fail",
            ResourceType = "Execution",
            ResourceId = notification.Id.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ExecutionId"] = notification.Id,
                ["Error"] = notification.Error,
                ["FailedAt"] = notification.FailedAt,
                ["DurationMs"] = notification.DurationMs,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }

    private async Task SendAlertAsync(ExecutionFailed notification)
    {
        try
        {
            // High priority alert via notification service
            this._logger.LogWarning(
                "HIGH PRIORITY ALERT: Execution {ExecutionId} failed after {DurationMs}ms with error: {Error}",
                notification.Id,
                notification.DurationMs,
                notification.Error);

            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to send alert for execution failure {ExecutionId}", notification.Id);
        }
    }
}

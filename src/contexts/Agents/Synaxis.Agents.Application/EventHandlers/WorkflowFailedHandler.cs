// <copyright file="WorkflowFailedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.EventHandlers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Domain.Events;
using Synaxis.Core.Contracts;

/// <summary>
/// Handles workflow failed events.
/// </summary>
public class WorkflowFailedHandler : INotificationHandler<WorkflowFailed>
{
    private readonly ILogger<WorkflowFailedHandler> _logger;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowFailedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    public WorkflowFailedHandler(
        ILogger<WorkflowFailedHandler> logger,
        IAuditService auditService)
    {
        this._logger = logger!;
        this._auditService = auditService!;
    }

    /// <summary>
    /// Handles the workflow failed notification.
    /// </summary>
    /// <param name="notification">The workflow failed notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public async ValueTask Handle(WorkflowFailed notification, CancellationToken cancellationToken)
    {
        this._logger.LogError(
            "Workflow failed: {Id} at step {StepNumber} - {Error}",
            notification.Id,
            notification.StepNumber,
            notification.Error);

        // Send high-priority alert
        await this.SendAlertAsync(notification).ConfigureAwait(false);

        // Audit log the failure
        var auditEvent = new AuditEvent
        {
            EventType = "Workflow.Failed",
            EventCategory = "Workflow",
            Action = "Fail",
            ResourceType = "Workflow",
            ResourceId = notification.Id.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["WorkflowId"] = notification.Id,
                ["StepNumber"] = notification.StepNumber,
                ["Error"] = notification.Error,
                ["FailedAt"] = notification.FailedAt,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }

    private async Task SendAlertAsync(WorkflowFailed notification)
    {
        try
        {
            // High priority alert via notification service
            // In production, this would use a dedicated alert service
            this._logger.LogWarning(
                "HIGH PRIORITY ALERT: Workflow {WorkflowId} failed at step {StepNumber} with error: {Error}",
                notification.Id,
                notification.StepNumber,
                notification.Error);

            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to send alert for workflow failure {WorkflowId}", notification.Id);
        }
    }
}

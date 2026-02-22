// <copyright file="WorkflowCreatedHandler.cs" company="Synaxis">
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
/// Handles workflow created events.
/// </summary>
public class WorkflowCreatedHandler : INotificationHandler<WorkflowCreated>
{
    private readonly ILogger<WorkflowCreatedHandler> _logger;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowCreatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    public WorkflowCreatedHandler(
        ILogger<WorkflowCreatedHandler> logger,
        IAuditService auditService)
    {
        this._logger = logger!;
        this._auditService = auditService!;
    }

    /// <summary>
    /// Handles the workflow created notification.
    /// </summary>
    /// <param name="notification">The workflow created notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public async ValueTask Handle(WorkflowCreated notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Workflow created: {Name} ({Id}) for tenant {TenantId}",
            notification.Name,
            notification.Id,
            notification.TenantId);

        var auditEvent = new AuditEvent
        {
            EventType = "Workflow.Created",
            EventCategory = "Workflow",
            Action = "Create",
            ResourceType = "Workflow",
            ResourceId = notification.Id.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["WorkflowName"] = notification.Name,
                ["TenantId"] = notification.TenantId,
                ["Version"] = notification.Version,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }
}

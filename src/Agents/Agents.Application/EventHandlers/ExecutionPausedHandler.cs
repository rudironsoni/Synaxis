// <copyright file="ExecutionPausedHandler.cs" company="Synaxis">
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
/// Handles execution paused events.
/// </summary>
public class ExecutionPausedHandler : INotificationHandler<ExecutionPaused>
{
    private readonly ILogger<ExecutionPausedHandler> _logger;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionPausedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    public ExecutionPausedHandler(
        ILogger<ExecutionPausedHandler> logger,
        IAuditService auditService)
    {
        this._logger = logger!;
        this._auditService = auditService!;
    }

    /// <inheritdoc/>
    public async ValueTask Handle(ExecutionPaused notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Execution paused: {ExecutionId} at step {CurrentStep}",
            notification.Id,
            notification.CurrentStep);

        // Audit log the pause
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ExecutionPaused),
            EventCategory = "Execution",
            Action = "Pause",
            ResourceType = "Execution",
            ResourceId = notification.Id.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ExecutionId"] = notification.Id,
                ["CurrentStep"] = notification.CurrentStep,
                ["PausedAt"] = notification.OccurredOn,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }
}

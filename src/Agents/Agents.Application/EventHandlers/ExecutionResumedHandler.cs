// <copyright file="ExecutionResumedHandler.cs" company="Synaxis">
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
/// Handles execution resumed events.
/// </summary>
public class ExecutionResumedHandler : INotificationHandler<ExecutionResumed>
{
    private readonly ILogger<ExecutionResumedHandler> _logger;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionResumedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    public ExecutionResumedHandler(
        ILogger<ExecutionResumedHandler> logger,
        IAuditService auditService)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    /// <inheritdoc/>
    public async ValueTask Handle(ExecutionResumed notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Execution resumed: {ExecutionId} at step {CurrentStep}",
            notification.Id,
            notification.CurrentStep);

        // Audit log the resume
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ExecutionResumed),
            EventCategory = "Execution",
            Action = "Resume",
            ResourceType = "Execution",
            ResourceId = notification.Id.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ExecutionId"] = notification.Id,
                ["CurrentStep"] = notification.CurrentStep,
                ["ResumedAt"] = notification.OccurredOn,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }
}

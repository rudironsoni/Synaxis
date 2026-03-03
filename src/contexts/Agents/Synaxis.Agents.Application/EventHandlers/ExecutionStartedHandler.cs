// <copyright file="ExecutionStartedHandler.cs" company="Synaxis">
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
/// Handles execution started events.
/// </summary>
public class ExecutionStartedHandler : INotificationHandler<ExecutionStarted>
{
    private readonly ILogger<ExecutionStartedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly IExecutionMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStartedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="metrics">The execution metrics service.</param>
    public ExecutionStartedHandler(
        ILogger<ExecutionStartedHandler> logger,
        IAuditService auditService,
        IExecutionMetrics metrics)
    {
        this._logger = logger!;
        this._auditService = auditService!;
        this._metrics = metrics!;
    }

    /// <inheritdoc/>
    public async ValueTask Handle(ExecutionStarted notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Execution started: {ExecutionId} for agent {AgentId}",
            notification.ExecutionId,
            notification.AgentId);

        // Record metrics
        await this._metrics.RecordExecutionStartedAsync(
            notification.Id,
            notification.AgentId,
            cancellationToken).ConfigureAwait(false);

        // Audit log the start
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ExecutionStarted),
            EventCategory = "Execution",
            Action = "Start",
            ResourceType = "Execution",
            ResourceId = notification.Id.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ExecutionId"] = notification.ExecutionId,
                ["AgentId"] = notification.AgentId,
                ["StartedAt"] = notification.StartedAt,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }
}

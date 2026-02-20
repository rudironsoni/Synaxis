// <copyright file="ExecutionCompletedHandler.cs" company="Synaxis">
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
/// Handles execution completed events.
/// </summary>
public class ExecutionCompletedHandler : INotificationHandler<ExecutionCompleted>
{
    private readonly ILogger<ExecutionCompletedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly IExecutionMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionCompletedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="metrics">The execution metrics service.</param>
    public ExecutionCompletedHandler(
        ILogger<ExecutionCompletedHandler> logger,
        IAuditService auditService,
        IExecutionMetrics metrics)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        this._metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <inheritdoc/>
    public async ValueTask Handle(ExecutionCompleted notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Execution completed: {ExecutionId} in {DurationMs}ms",
            notification.Id,
            notification.DurationMs);

        // Update metrics
        await this._metrics.RecordExecutionCompletedAsync(
            notification.Id,
            notification.DurationMs,
            cancellationToken).ConfigureAwait(false);

        // Audit log the completion
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ExecutionCompleted),
            EventCategory = "Execution",
            Action = "Complete",
            ResourceType = "Execution",
            ResourceId = notification.Id.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ExecutionId"] = notification.Id,
                ["CompletedAt"] = notification.CompletedAt,
                ["DurationMs"] = notification.DurationMs,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }
}

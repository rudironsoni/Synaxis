// <copyright file="ExecutionCancelledHandler.cs" company="Synaxis">
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
/// Handles execution cancelled events.
/// </summary>
public class ExecutionCancelledHandler : INotificationHandler<ExecutionCancelled>
{
    private readonly ILogger<ExecutionCancelledHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly IExecutionMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionCancelledHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="metrics">The execution metrics service.</param>
    public ExecutionCancelledHandler(
        ILogger<ExecutionCancelledHandler> logger,
        IAuditService auditService,
        IExecutionMetrics metrics)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        this._metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <inheritdoc/>
    public async ValueTask Handle(ExecutionCancelled notification, CancellationToken cancellationToken)
    {
        this._logger.LogWarning(
            "Execution cancelled: {ExecutionId} after {DurationMs}ms",
            notification.Id,
            notification.DurationMs);

        // Update metrics
        await this._metrics.RecordExecutionCancelledAsync(
            notification.Id,
            notification.DurationMs,
            cancellationToken).ConfigureAwait(false);

        // Audit log the cancellation
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ExecutionCancelled),
            EventCategory = "Execution",
            Action = "Cancel",
            ResourceType = "Execution",
            ResourceId = notification.Id.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ExecutionId"] = notification.Id,
                ["CurrentStep"] = notification.CurrentStep,
                ["CancelledAt"] = notification.CancelledAt,
                ["DurationMs"] = notification.DurationMs,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }
}

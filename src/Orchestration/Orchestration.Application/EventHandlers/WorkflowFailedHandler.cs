// <copyright file="WorkflowFailedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Application.EventHandlers;

using MediatR;
using Microsoft.Extensions.Logging;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Handles workflow failed events.
/// </summary>
public class WorkflowFailedHandler : INotificationHandler<WorkflowFailed>
{
    private readonly ILogger<WorkflowFailedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowFailedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WorkflowFailedHandler(ILogger<WorkflowFailedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(WorkflowFailed notification, CancellationToken cancellationToken)
    {
        this._logger.LogError(
            "Workflow {WorkflowId} failed: {ErrorMessage}",
            notification.WorkflowId,
            notification.ErrorMessage);

        // In production, this could:
        // - Send alerts
        // - Trigger compensation
        // - Update monitoring
        return Task.CompletedTask;
    }
}

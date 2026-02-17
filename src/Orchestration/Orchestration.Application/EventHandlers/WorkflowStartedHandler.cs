// <copyright file="WorkflowStartedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Application.EventHandlers;

using MediatR;
using Microsoft.Extensions.Logging;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Handles workflow started events.
/// </summary>
public class WorkflowStartedHandler : INotificationHandler<WorkflowStarted>
{
    private readonly ILogger<WorkflowStartedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowStartedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WorkflowStartedHandler(ILogger<WorkflowStartedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(WorkflowStarted notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Workflow {WorkflowId} started", notification.WorkflowId);
        return Task.CompletedTask;
    }
}

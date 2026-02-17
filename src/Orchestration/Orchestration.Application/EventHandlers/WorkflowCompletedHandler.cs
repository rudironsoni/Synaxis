// <copyright file="WorkflowCompletedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Application.EventHandlers;

using MediatR;
using Microsoft.Extensions.Logging;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Handles workflow completed events.
/// </summary>
public class WorkflowCompletedHandler : INotificationHandler<WorkflowCompleted>
{
    private readonly ILogger<WorkflowCompletedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowCompletedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WorkflowCompletedHandler(ILogger<WorkflowCompletedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Workflow {WorkflowId} completed", notification.WorkflowId);
        return Task.CompletedTask;
    }
}

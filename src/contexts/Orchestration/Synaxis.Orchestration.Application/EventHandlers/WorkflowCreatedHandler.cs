// <copyright file="WorkflowCreatedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Application.EventHandlers;

using MediatR;
using Microsoft.Extensions.Logging;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Handles workflow created events.
/// </summary>
public class WorkflowCreatedHandler : INotificationHandler<WorkflowCreated>
{
    private readonly ILogger<WorkflowCreatedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowCreatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WorkflowCreatedHandler(ILogger<WorkflowCreatedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(WorkflowCreated notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Workflow {WorkflowId} created for tenant {TenantId}",
            notification.WorkflowId,
            notification.TenantId);

        // In production, this could:
        // - Send notifications
        // - Update read models
        // - Trigger integrations
        return Task.CompletedTask;
    }
}

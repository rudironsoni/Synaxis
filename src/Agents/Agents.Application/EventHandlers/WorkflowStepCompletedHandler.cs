// <copyright file="WorkflowStepCompletedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.EventHandlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Domain.Events;

/// <summary>
/// Handles workflow step completed events.
/// </summary>
public class WorkflowStepCompletedHandler : INotificationHandler<WorkflowStepCompleted>
{
    private readonly ILogger<WorkflowStepCompletedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowStepCompletedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WorkflowStepCompletedHandler(ILogger<WorkflowStepCompletedHandler> logger)
    {
        this._logger = logger!;
    }

    /// <summary>
    /// Handles the workflow step completed notification.
    /// </summary>
    /// <param name="notification">The workflow step completed notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public ValueTask Handle(WorkflowStepCompleted notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Workflow step completed: {Id} - Step {StepNumber} ({StepName})",
            notification.Id,
            notification.StepNumber,
            notification.StepName);

        // Metrics recording would be done via injected metrics service
        return default;
    }
}

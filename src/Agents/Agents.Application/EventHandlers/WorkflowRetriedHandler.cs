// <copyright file="WorkflowRetriedHandler.cs" company="Synaxis">
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
/// Handles workflow retried events.
/// </summary>
public class WorkflowRetriedHandler : INotificationHandler<WorkflowRetried>
{
    private readonly ILogger<WorkflowRetriedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowRetriedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WorkflowRetriedHandler(ILogger<WorkflowRetriedHandler> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the workflow retried notification.
    /// </summary>
    /// <param name="notification">The workflow retried notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public ValueTask Handle(WorkflowRetried notification, CancellationToken cancellationToken)
    {
        this._logger.LogWarning(
            "Workflow retried: {Id} at step {StepNumber}, attempt {RetryAttempt}",
            notification.Id,
            notification.StepNumber,
            notification.RetryAttempt);

        return default;
    }
}

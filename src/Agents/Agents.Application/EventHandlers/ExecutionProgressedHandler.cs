// <copyright file="ExecutionProgressedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.EventHandlers;

using System;
using System.Threading;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Domain.Events;

/// <summary>
/// Handles execution progressed events.
/// </summary>
public class ExecutionProgressedHandler : INotificationHandler<ExecutionProgressed>
{
    private readonly ILogger<ExecutionProgressedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionProgressedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ExecutionProgressedHandler(ILogger<ExecutionProgressedHandler> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public ValueTask Handle(ExecutionProgressed notification, CancellationToken cancellationToken)
    {
        this._logger.LogDebug(
            "Execution progressed: {ExecutionId} to step {CurrentStep}",
            notification.Id,
            notification.CurrentStep);

        return default;
    }
}

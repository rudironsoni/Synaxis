// <copyright file="SagaFailedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Application.EventHandlers;

using MediatR;
using Microsoft.Extensions.Logging;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Handles saga failed events.
/// </summary>
public class SagaFailedHandler : INotificationHandler<SagaFailed>
{
    private readonly ILogger<SagaFailedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaFailedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SagaFailedHandler(ILogger<SagaFailedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(SagaFailed notification, CancellationToken cancellationToken)
    {
        this._logger.LogError(
            "Saga {SagaId} failed at activity {ActivityId}: {ErrorMessage}",
            notification.SagaId,
            notification.FailedActivityId,
            notification.ErrorMessage);

        // Trigger compensation process
        return Task.CompletedTask;
    }
}

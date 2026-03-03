// <copyright file="SagaCreatedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Application.EventHandlers;

using MediatR;
using Microsoft.Extensions.Logging;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Handles saga created events.
/// </summary>
public class SagaCreatedHandler : INotificationHandler<SagaCreated>
{
    private readonly ILogger<SagaCreatedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaCreatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SagaCreatedHandler(ILogger<SagaCreatedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(SagaCreated notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Saga {SagaId} created for tenant {TenantId}",
            notification.SagaId,
            notification.TenantId);

        return Task.CompletedTask;
    }
}

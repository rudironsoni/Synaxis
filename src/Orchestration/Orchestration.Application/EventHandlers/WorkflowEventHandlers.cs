// <copyright file="WorkflowEventHandlers.cs" company="Synaxis">
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

// <copyright file="ModelConfigActivatedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.EventHandlers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Core.Contracts;
using Synaxis.Inference.Domain.Events;

/// <summary>
/// Handles model configuration activated events.
/// </summary>
public class ModelConfigActivatedHandler : INotificationHandler<ModelConfigActivated>
{
    private readonly ILogger<ModelConfigActivatedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;
    private readonly IMessageBus? _messageBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelConfigActivatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="messageBus">The message bus (optional).</param>
    public ModelConfigActivatedHandler(
        ILogger<ModelConfigActivatedHandler> logger,
        IAuditService auditService,
        ICacheService cacheService,
        IMessageBus? messageBus = null)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        this._cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        this._messageBus = messageBus;
    }

    /// <inheritdoc/>
    public async ValueTask Handle(ModelConfigActivated notification, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        cancellationToken.ThrowIfCancellationRequested();

        this._logger.LogInformation(
            "Model configuration activated: {ConfigId}",
            notification.ConfigId);

        await this.EnableForRoutingAsync(notification).ConfigureAwait(false);
        await this.LogAuditEventAsync(notification).ConfigureAwait(false);

        if (this._messageBus is not null)
        {
            await this.PublishToMessageBusAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask EnableForRoutingAsync(ModelConfigActivated notification)
    {
        try
        {
            var routingCacheKey = $"routing:active-models";
            await this._cacheService.RemoveByPatternAsync(routingCacheKey).ConfigureAwait(false);

            var configCacheKey = $"modelconfig:{notification.ConfigId}";
            await this._cacheService.RemoveAsync(configCacheKey).ConfigureAwait(false);

            this._logger.LogDebug(
                "Model {ConfigId} enabled for routing",
                notification.ConfigId);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to enable model {ConfigId} for routing",
                notification.ConfigId);
        }
    }

    private async ValueTask LogAuditEventAsync(ModelConfigActivated notification)
    {
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ModelConfigActivated),
            EventCategory = "ModelConfig",
            Action = "Activate",
            ResourceType = "ModelConfiguration",
            ResourceId = notification.ConfigId.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ConfigId"] = notification.ConfigId,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }

    private async ValueTask PublishToMessageBusAsync(
        ModelConfigActivated notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._messageBus!.PublishAsync(
                "models.activated",
                notification,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to publish model config activated event to message bus for config {ConfigId}",
                notification.ConfigId);
        }
    }
}

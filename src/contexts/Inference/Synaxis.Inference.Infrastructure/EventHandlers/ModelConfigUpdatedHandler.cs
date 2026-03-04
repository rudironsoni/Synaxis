// <copyright file="ModelConfigUpdatedHandler.cs" company="Synaxis">
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
/// Handles model configuration updated events.
/// </summary>
public class ModelConfigUpdatedHandler : INotificationHandler<ModelConfigUpdated>
{
    private readonly ILogger<ModelConfigUpdatedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;
    private readonly IMessageBus? _messageBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelConfigUpdatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="messageBus">The message bus (optional).</param>
    public ModelConfigUpdatedHandler(
        ILogger<ModelConfigUpdatedHandler> logger,
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
    public async ValueTask Handle(ModelConfigUpdated notification, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        cancellationToken.ThrowIfCancellationRequested();

        this._logger.LogInformation(
            "Model configuration updated: {DisplayName} ({ConfigId})",
            notification.DisplayName,
            notification.ConfigId);

        await this.LogAuditEventAsync(notification).ConfigureAwait(false);
        await this.UpdateProviderCacheAsync(notification).ConfigureAwait(false);

        if (this._messageBus is not null)
        {
            await this.PublishToMessageBusAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask LogAuditEventAsync(ModelConfigUpdated notification)
    {
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ModelConfigUpdated),
            EventCategory = "ModelConfig",
            Action = "Update",
            ResourceType = "ModelConfiguration",
            ResourceId = notification.ConfigId.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ConfigId"] = notification.ConfigId,
                ["DisplayName"] = notification.DisplayName,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }

    private async ValueTask UpdateProviderCacheAsync(ModelConfigUpdated notification)
    {
        try
        {
            var configCacheKey = $"modelconfig:{notification.ConfigId}";
            await this._cacheService.RemoveAsync(configCacheKey).ConfigureAwait(false);

            var listCacheKey = "modelconfig:list";
            await this._cacheService.RemoveByPatternAsync(listCacheKey).ConfigureAwait(false);

            this._logger.LogDebug(
                "Provider cache updated for config {ConfigId}",
                notification.ConfigId);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to update provider cache for config {ConfigId}",
                notification.ConfigId);
        }
    }

    private async ValueTask PublishToMessageBusAsync(
        ModelConfigUpdated notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._messageBus!.PublishAsync(
                "models.updated",
                notification,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to publish model config updated event to message bus for config {ConfigId}",
                notification.ConfigId);
        }
    }
}

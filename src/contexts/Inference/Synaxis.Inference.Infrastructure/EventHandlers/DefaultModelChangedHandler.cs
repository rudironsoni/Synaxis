// <copyright file="DefaultModelChangedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.EventHandlers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Shared.Kernel.Application.Cloud;
using Synaxis.Shared.Kernel.Domain.Contracts;
using Synaxis.Inference.Domain.Events;

/// <summary>
/// Handles default model changed events.
/// </summary>
public class DefaultModelChangedHandler : INotificationHandler<PreferredModelUpdated>
{
    private readonly ILogger<DefaultModelChangedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;
    private readonly IMessageBus? _messageBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultModelChangedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="messageBus">The message bus (optional).</param>
    public DefaultModelChangedHandler(
        ILogger<DefaultModelChangedHandler> logger,
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
    public async ValueTask Handle(PreferredModelUpdated notification, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        cancellationToken.ThrowIfCancellationRequested();

        this._logger.LogInformation(
            "Default model changed: {PreferencesId} - new default model {ModelId} from provider {ProviderId}",
            notification.PreferencesId,
            notification.ModelId,
            notification.ProviderId);

        await this.UpdateRoutingPreferencesAsync(notification).ConfigureAwait(false);
        await this.LogAuditEventAsync(notification).ConfigureAwait(false);

        if (this._messageBus is not null)
        {
            await this.PublishToMessageBusAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask UpdateRoutingPreferencesAsync(PreferredModelUpdated notification)
    {
        try
        {
            var routingCacheKey = $"routing:preferences:{notification.PreferencesId}";
            await this._cacheService.RemoveAsync(routingCacheKey).ConfigureAwait(false);

            var defaultModelCacheKey = "routing:default-model";
            await this._cacheService.RemoveByPatternAsync(defaultModelCacheKey).ConfigureAwait(false);

            var userRoutingKey = $"user-routing:{notification.PreferencesId}";
            await this._cacheService.RemoveAsync(userRoutingKey).ConfigureAwait(false);

            this._logger.LogDebug(
                "Routing preferences updated for {PreferencesId}",
                notification.PreferencesId);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to update routing preferences for {PreferencesId}",
                notification.PreferencesId);
        }
    }

    private async ValueTask LogAuditEventAsync(PreferredModelUpdated notification)
    {
        var auditEvent = new AuditEvent
        {
            EventType = nameof(PreferredModelUpdated),
            EventCategory = "UserChatPreferences",
            Action = "DefaultModelChanged",
            ResourceType = "UserChatPreferences",
            ResourceId = notification.PreferencesId.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["PreferencesId"] = notification.PreferencesId,
                ["ModelId"] = notification.ModelId ?? string.Empty,
                ["ProviderId"] = notification.ProviderId ?? string.Empty,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }

    private async ValueTask PublishToMessageBusAsync(
        PreferredModelUpdated notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._messageBus!.PublishAsync(
                "preferences.default-model-changed",
                notification,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to publish default model changed event to message bus for preferences {PreferencesId}",
                notification.PreferencesId);
        }
    }
}

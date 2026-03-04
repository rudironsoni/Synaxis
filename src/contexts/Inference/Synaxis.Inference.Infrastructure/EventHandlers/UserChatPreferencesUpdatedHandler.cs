// <copyright file="UserChatPreferencesUpdatedHandler.cs" company="Synaxis">
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
/// Handles user chat preferences updated events.
/// </summary>
public class UserChatPreferencesUpdatedHandler : INotificationHandler<PreferredModelUpdated>
{
    private readonly ILogger<UserChatPreferencesUpdatedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;
    private readonly IMessageBus? _messageBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserChatPreferencesUpdatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="messageBus">The message bus (optional).</param>
    public UserChatPreferencesUpdatedHandler(
        ILogger<UserChatPreferencesUpdatedHandler> logger,
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
            "User chat preferences updated: {PreferencesId} - preferred model changed to {ModelId}",
            notification.PreferencesId,
            notification.ModelId);

        await this.UpdateDefaultsAsync(notification).ConfigureAwait(false);
        await this.LogAuditEventAsync(notification).ConfigureAwait(false);

        if (this._messageBus is not null)
        {
            await this.PublishToMessageBusAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask UpdateDefaultsAsync(PreferredModelUpdated notification)
    {
        try
        {
            var preferencesCacheKey = $"user-preferences:{notification.PreferencesId}";
            await this._cacheService.RemoveAsync(preferencesCacheKey).ConfigureAwait(false);

            var defaultsCacheKey = "user-preferences:defaults";
            await this._cacheService.RemoveByPatternAsync(defaultsCacheKey).ConfigureAwait(false);

            this._logger.LogDebug(
                "Defaults updated for preferences {PreferencesId}",
                notification.PreferencesId);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to update defaults for preferences {PreferencesId}",
                notification.PreferencesId);
        }
    }

    private async ValueTask LogAuditEventAsync(PreferredModelUpdated notification)
    {
        var auditEvent = new AuditEvent
        {
            EventType = nameof(PreferredModelUpdated),
            EventCategory = "UserChatPreferences",
            Action = "Update",
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
                "preferences.updated",
                notification,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to publish preferences updated event to message bus for preferences {PreferencesId}",
                notification.PreferencesId);
        }
    }
}

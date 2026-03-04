// <copyright file="ChatTemplateUpdatedHandler.cs" company="Synaxis">
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
/// Handles chat template updated events.
/// </summary>
public class ChatTemplateUpdatedHandler : INotificationHandler<ChatTemplateUpdated>
{
    private readonly ILogger<ChatTemplateUpdatedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;
    private readonly IMessageBus? _messageBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatTemplateUpdatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="messageBus">The message bus (optional).</param>
    public ChatTemplateUpdatedHandler(
        ILogger<ChatTemplateUpdatedHandler> logger,
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
    public async ValueTask Handle(ChatTemplateUpdated notification, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        cancellationToken.ThrowIfCancellationRequested();

        this._logger.LogInformation(
            "Chat template updated: {TemplateName} ({TemplateId})",
            notification.Name,
            notification.TemplateId);

        await this.LogAuditEventAsync(notification).ConfigureAwait(false);
        await this.InvalidateTemplateCacheAsync(notification).ConfigureAwait(false);

        if (this._messageBus is not null)
        {
            await this.PublishToMessageBusAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask LogAuditEventAsync(ChatTemplateUpdated notification)
    {
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ChatTemplateUpdated),
            EventCategory = "Template",
            Action = "Update",
            ResourceType = "ChatTemplate",
            ResourceId = notification.TemplateId.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["TemplateId"] = notification.TemplateId,
                ["Name"] = notification.Name,
                ["Category"] = notification.Category,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }

    private async ValueTask InvalidateTemplateCacheAsync(ChatTemplateUpdated notification)
    {
        try
        {
            var cacheKey = $"template:{notification.TemplateId}";
            await this._cacheService.RemoveAsync(cacheKey).ConfigureAwait(false);

            var categoryCacheKey = $"templates:category:{notification.Category}";
            await this._cacheService.RemoveByPatternAsync(categoryCacheKey).ConfigureAwait(false);

            var listCacheKey = "templates:list";
            await this._cacheService.RemoveByPatternAsync(listCacheKey).ConfigureAwait(false);

            this._logger.LogDebug(
                "Cache invalidated for template {TemplateId}",
                notification.TemplateId);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to invalidate cache for template {TemplateId}",
                notification.TemplateId);
        }
    }

    private async ValueTask PublishToMessageBusAsync(
        ChatTemplateUpdated notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._messageBus!.PublishAsync(
                "templates.updated",
                notification,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to publish template updated event to message bus for template {TemplateId}",
                notification.TemplateId);
        }
    }
}

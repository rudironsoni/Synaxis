// <copyright file="ChatTemplateDeletedHandler.cs" company="Synaxis">
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
/// Handles chat template deleted events.
/// </summary>
public class ChatTemplateDeletedHandler : INotificationHandler<ChatTemplateDeleted>
{
    private readonly ILogger<ChatTemplateDeletedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;
    private readonly IMessageBus? _messageBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatTemplateDeletedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="messageBus">The message bus (optional).</param>
    public ChatTemplateDeletedHandler(
        ILogger<ChatTemplateDeletedHandler> logger,
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
    public async ValueTask Handle(ChatTemplateDeleted notification, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        cancellationToken.ThrowIfCancellationRequested();

        this._logger.LogInformation(
            "Chat template deleted: {TemplateId}",
            notification.TemplateId);

        await this.LogAuditEventAsync(notification).ConfigureAwait(false);
        await this.CleanupRelatedDataAsync(notification).ConfigureAwait(false);
        await this.InvalidateTemplateCacheAsync(notification).ConfigureAwait(false);

        if (this._messageBus is not null)
        {
            await this.PublishToMessageBusAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask LogAuditEventAsync(ChatTemplateDeleted notification)
    {
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ChatTemplateDeleted),
            EventCategory = "Template",
            Action = "Delete",
            ResourceType = "ChatTemplate",
            ResourceId = notification.TemplateId.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["TemplateId"] = notification.TemplateId,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }

    private async ValueTask CleanupRelatedDataAsync(ChatTemplateDeleted notification)
    {
        try
        {
            var usageCacheKey = $"template:usage:{notification.TemplateId}";
            await this._cacheService.RemoveAsync(usageCacheKey).ConfigureAwait(false);

            var statsCacheKey = $"template:stats:{notification.TemplateId}";
            await this._cacheService.RemoveAsync(statsCacheKey).ConfigureAwait(false);

            this._logger.LogDebug(
                "Related data cleaned up for template {TemplateId}",
                notification.TemplateId);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to cleanup related data for template {TemplateId}",
                notification.TemplateId);
        }
    }

    private async ValueTask InvalidateTemplateCacheAsync(ChatTemplateDeleted notification)
    {
        try
        {
            var cacheKey = $"template:{notification.TemplateId}";
            await this._cacheService.RemoveAsync(cacheKey).ConfigureAwait(false);

            var listCacheKey = "templates:list";
            await this._cacheService.RemoveByPatternAsync(listCacheKey).ConfigureAwait(false);

            this._logger.LogDebug(
                "Cache invalidated for deleted template {TemplateId}",
                notification.TemplateId);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to invalidate cache for deleted template {TemplateId}",
                notification.TemplateId);
        }
    }

    private async ValueTask PublishToMessageBusAsync(
        ChatTemplateDeleted notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._messageBus!.PublishAsync(
                "templates.deleted",
                notification,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to publish template deleted event to message bus for template {TemplateId}",
                notification.TemplateId);
        }
    }
}

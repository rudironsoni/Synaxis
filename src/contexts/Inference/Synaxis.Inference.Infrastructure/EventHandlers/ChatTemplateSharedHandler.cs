// <copyright file="ChatTemplateSharedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.EventHandlers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Shared.Kernel.Domain.Contracts;
using Synaxis.Inference.Domain.Events;

/// <summary>
/// Handles chat template shared events.
/// </summary>
public class ChatTemplateSharedHandler : INotificationHandler<ChatTemplateUsed>
{
    private readonly ILogger<ChatTemplateSharedHandler> _logger;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatTemplateSharedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    public ChatTemplateSharedHandler(
        ILogger<ChatTemplateSharedHandler> logger,
        IAuditService auditService)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    /// <inheritdoc/>
    public async ValueTask Handle(ChatTemplateUsed notification, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        cancellationToken.ThrowIfCancellationRequested();

        this._logger.LogInformation(
            "Chat template used: {TemplateId}",
            notification.TemplateId);

        await this.LogAuditEventAsync(notification).ConfigureAwait(false);
        await this.SendNotificationAsync(notification).ConfigureAwait(false);
    }

    private async ValueTask LogAuditEventAsync(ChatTemplateUsed notification)
    {
        var auditEvent = new AuditEvent
        {
            EventType = nameof(ChatTemplateUsed),
            EventCategory = "Template",
            Action = "Use",
            ResourceType = "ChatTemplate",
            ResourceId = notification.TemplateId.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["TemplateId"] = notification.TemplateId,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }

    private ValueTask SendNotificationAsync(ChatTemplateUsed notification)
    {
        this._logger.LogInformation(
            "Notification: Template {TemplateId} was used",
            notification.TemplateId);

        return default;
    }
}

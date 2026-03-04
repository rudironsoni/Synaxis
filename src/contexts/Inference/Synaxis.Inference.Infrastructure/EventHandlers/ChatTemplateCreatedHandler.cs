// <copyright file="ChatTemplateCreatedHandler.cs" company="Synaxis">
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
/// Handles chat template created events.
/// </summary>
public class ChatTemplateCreatedHandler : INotificationHandler<ChatTemplateCreated>
{
    private readonly ILogger<ChatTemplateCreatedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly IMessageBus? _messageBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatTemplateCreatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="messageBus">The message bus (optional).</param>
    public ChatTemplateCreatedHandler(
        ILogger<ChatTemplateCreatedHandler> logger,
        IAuditService auditService,
        IMessageBus? messageBus = null)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        this._messageBus = messageBus;
    }

    /// <inheritdoc/>
    public async ValueTask Handle(ChatTemplateCreated notification, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        cancellationToken.ThrowIfCancellationRequested();

        this._logger.LogInformation(
            "Chat template created: {TemplateName} ({TemplateId}) for tenant {TenantId}",
            notification.Name,
            notification.TemplateId,
            notification.TenantId);

        await this.LogAuditEventAsync(notification).ConfigureAwait(false);

        if (this._messageBus is not null)
        {
            await this.PublishToMessageBusAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask LogAuditEventAsync(ChatTemplateCreated notification)
    {
        var auditEvent = new AuditEvent
        {
            OrganizationId = notification.TenantId,
            EventType = nameof(ChatTemplateCreated),
            EventCategory = "Template",
            Action = "Create",
            ResourceType = "ChatTemplate",
            ResourceId = notification.TemplateId.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["TemplateId"] = notification.TemplateId,
                ["Name"] = notification.Name,
                ["Category"] = notification.Category,
                ["IsSystemTemplate"] = notification.IsSystemTemplate,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }

    private async ValueTask PublishToMessageBusAsync(
        ChatTemplateCreated notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._messageBus!.PublishAsync(
                "templates.created",
                notification,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to publish template created event to message bus for template {TemplateId}",
                notification.TemplateId);
        }
    }
}

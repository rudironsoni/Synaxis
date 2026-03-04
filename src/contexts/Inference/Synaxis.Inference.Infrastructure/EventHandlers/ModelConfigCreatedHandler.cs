// <copyright file="ModelConfigCreatedHandler.cs" company="Synaxis">
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
/// Handles model configuration created events.
/// </summary>
public class ModelConfigCreatedHandler : INotificationHandler<ModelConfigCreated>
{
    private readonly ILogger<ModelConfigCreatedHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly IMessageBus? _messageBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelConfigCreatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="messageBus">The message bus (optional).</param>
    public ModelConfigCreatedHandler(
        ILogger<ModelConfigCreatedHandler> logger,
        IAuditService auditService,
        IMessageBus? messageBus = null)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        this._messageBus = messageBus;
    }

    /// <inheritdoc/>
    public async ValueTask Handle(ModelConfigCreated notification, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        cancellationToken.ThrowIfCancellationRequested();

        this._logger.LogInformation(
            "Model configuration created: {DisplayName} ({ConfigId}) for tenant {TenantId}",
            notification.DisplayName,
            notification.ConfigId,
            notification.TenantId);

        await this.ValidateConfigurationAsync(notification).ConfigureAwait(false);
        await this.LogAuditEventAsync(notification).ConfigureAwait(false);

        if (this._messageBus is not null)
        {
            await this.PublishToMessageBusAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task ValidateConfigurationAsync(ModelConfigCreated notification)
    {
        if (string.IsNullOrWhiteSpace(notification.ModelId))
        {
            this._logger.LogWarning(
                "Model configuration {ConfigId} has empty model identifier",
                notification.ConfigId);
        }

        if (string.IsNullOrWhiteSpace(notification.ProviderId))
        {
            this._logger.LogWarning(
                "Model configuration {ConfigId} has empty provider identifier",
                notification.ConfigId);
        }

        if (notification.Pricing is null)
        {
            this._logger.LogWarning(
                "Model configuration {ConfigId} has no pricing information",
                notification.ConfigId);
        }

        this._logger.LogDebug(
            "Configuration validation completed for {ConfigId}",
            notification.ConfigId);

        return Task.CompletedTask;
    }

    private async ValueTask LogAuditEventAsync(ModelConfigCreated notification)
    {
        var auditEvent = new AuditEvent
        {
            OrganizationId = notification.TenantId,
            EventType = nameof(ModelConfigCreated),
            EventCategory = "ModelConfig",
            Action = "Create",
            ResourceType = "ModelConfiguration",
            ResourceId = notification.ConfigId.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ConfigId"] = notification.ConfigId,
                ["ModelId"] = notification.ModelId,
                ["ProviderId"] = notification.ProviderId,
                ["DisplayName"] = notification.DisplayName,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }

    private async ValueTask PublishToMessageBusAsync(
        ModelConfigCreated notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._messageBus!.PublishAsync(
                "models.created",
                notification,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to publish model config created event to message bus for config {ConfigId}",
                notification.ConfigId);
        }
    }
}

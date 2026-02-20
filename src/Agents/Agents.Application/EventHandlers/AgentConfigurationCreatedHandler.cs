// <copyright file="AgentConfigurationCreatedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.EventHandlers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Domain.Events;
using Synaxis.Core.Contracts;

/// <summary>
/// Handles agent configuration created events.
/// </summary>
public class AgentConfigurationCreatedHandler : INotificationHandler<AgentConfigurationCreated>
{
    private readonly ILogger<AgentConfigurationCreatedHandler> _logger;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigurationCreatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditService">The audit service.</param>
    public AgentConfigurationCreatedHandler(
        ILogger<AgentConfigurationCreatedHandler> logger,
        IAuditService auditService)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    /// <inheritdoc/>
    public async ValueTask Handle(AgentConfigurationCreated notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Agent configuration created: {Name} ({AgentId})",
            notification.Name,
            notification.AgentId);

        var auditEvent = new AuditEvent
        {
            OrganizationId = notification.TenantId,
            UserId = notification.UserId,
            EventType = nameof(AgentConfigurationCreated),
            EventCategory = "AgentConfiguration",
            Action = "Create",
            ResourceType = "AgentConfiguration",
            ResourceId = notification.AgentId.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["AgentId"] = notification.AgentId,
                ["Name"] = notification.Name,
                ["AgentType"] = notification.AgentType,
                ["Version"] = notification.Version,
            },
        };

        await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
    }
}

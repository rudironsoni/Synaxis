// <copyright file="AgentConfigurationDeletedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.EventHandlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Domain.Events;

/// <summary>
/// Handles agent configuration deleted events.
/// </summary>
public class AgentConfigurationDeletedHandler : INotificationHandler<AgentConfigurationDeleted>
{
    private readonly ILogger<AgentConfigurationDeletedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigurationDeletedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AgentConfigurationDeletedHandler(ILogger<AgentConfigurationDeletedHandler> logger)
    {
        this._logger = logger!;
    }

    /// <inheritdoc/>
    public ValueTask Handle(AgentConfigurationDeleted notification, CancellationToken cancellationToken)
    {
        this._logger.LogWarning(
            "Agent configuration deleted: {AgentId}",
            notification.AgentId);

        return default;
    }
}

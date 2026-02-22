// <copyright file="AgentConfigurationUpdatedHandler.cs" company="Synaxis">
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
/// Handles agent configuration updated events.
/// </summary>
public class AgentConfigurationUpdatedHandler : INotificationHandler<AgentConfigurationUpdated>
{
    private readonly ILogger<AgentConfigurationUpdatedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigurationUpdatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AgentConfigurationUpdatedHandler(ILogger<AgentConfigurationUpdatedHandler> logger)
    {
        this._logger = logger!;
    }

    /// <inheritdoc/>
    public ValueTask Handle(AgentConfigurationUpdated notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Agent configuration updated: {AgentId}",
            notification.AgentId);

        return default;
    }
}

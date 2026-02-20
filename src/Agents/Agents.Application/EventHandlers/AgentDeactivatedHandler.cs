// <copyright file="AgentDeactivatedHandler.cs" company="Synaxis">
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
/// Handles agent deactivated events.
/// </summary>
public class AgentDeactivatedHandler : INotificationHandler<AgentDeactivated>
{
    private readonly ILogger<AgentDeactivatedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentDeactivatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AgentDeactivatedHandler(ILogger<AgentDeactivatedHandler> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public ValueTask Handle(AgentDeactivated notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Agent deactivated: {AgentId}",
            notification.AgentId);

        return default;
    }
}

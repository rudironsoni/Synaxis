// <copyright file="AgentActivatedHandler.cs" company="Synaxis">
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
/// Handles agent activated events.
/// </summary>
public class AgentActivatedHandler : INotificationHandler<AgentActivated>
{
    private readonly ILogger<AgentActivatedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentActivatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AgentActivatedHandler(ILogger<AgentActivatedHandler> logger)
    {
        this._logger = logger!;
    }

    /// <inheritdoc/>
    public ValueTask Handle(AgentActivated notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Agent activated: {AgentId}",
            notification.AgentId);

        return default;
    }
}

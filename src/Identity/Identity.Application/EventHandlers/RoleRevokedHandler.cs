// <copyright file="RoleRevokedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
/// Handles RoleRevoked domain events.
/// </summary>
public sealed class RoleRevokedHandler : INotificationHandler<RoleRevoked>
{
    private readonly ILogger<RoleRevokedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleRevokedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public RoleRevokedHandler(ILogger<RoleRevokedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(RoleRevoked notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("RoleRevoked handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}

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
    </summary>
    <param name="logger">The logger instance.</param>
    <summary>
/// Handles RoleRevoked domain events.
/// </summary>
public sealed class RoleRevokedHandler : INotificationHandler<RoleRevoked>
{
    private readonly ILogger<RoleRevokedHandler> logger;

    /// <summary>
    </summary>
    <param name="logger">The logger instance.</param>
    <summary>
    /// Initializes a new instance of the <see cref="RoleRevokedHandler"/> class.
    /// </summary>
    public RoleRevokedHandler(ILogger<RoleRevokedHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(RoleRevoked notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("RoleRevoked handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}

// <copyright file="PermissionRevokedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
/// Handles PermissionRevoked domain events.
/// </summary>
public sealed class PermissionRevokedHandler : INotificationHandler<PermissionRevoked>
{
    private readonly ILogger<PermissionRevokedHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionRevokedHandler"/> class.
    /// </summary>
    public PermissionRevokedHandler(ILogger<PermissionRevokedHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(PermissionRevoked notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation(
            "Permission {Permission} revoked from user {UserId} on resource {ResourceType}/{ResourceId}",
            notification.Permission,
            notification.UserId,
            notification.ResourceType,
            notification.ResourceId);

        return ValueTask.CompletedTask;
    }
}

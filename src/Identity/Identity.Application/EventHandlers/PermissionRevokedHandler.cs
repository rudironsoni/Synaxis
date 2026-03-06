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
    private readonly ILogger<PermissionRevokedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionRevokedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PermissionRevokedHandler(ILogger<PermissionRevokedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(PermissionRevoked notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("PermissionRevoked handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}

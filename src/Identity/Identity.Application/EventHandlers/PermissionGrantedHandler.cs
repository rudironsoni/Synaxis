// <copyright file="PermissionGrantedHandler.cs" company="Synaxis">
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
/// Handles PermissionGranted domain events.
/// </summary>
public sealed class PermissionGrantedHandler : INotificationHandler<PermissionGranted>
{
    private readonly ILogger<PermissionGrantedHandler> logger;

    /// <summary>
    </summary>
    <param name="logger">The logger instance.</param>
    <summary>
    /// Initializes a new instance of the <see cref="PermissionGrantedHandler"/> class.
    /// </summary>
    public PermissionGrantedHandler(ILogger<PermissionGrantedHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(PermissionGranted notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation(
            "Permission {Permission} granted to user {UserId} on resource {ResourceType}/{ResourceId}",
            notification.Permission,
            notification.UserId,
            notification.ResourceType,
            notification.ResourceId);

        return ValueTask.CompletedTask;
    }
}

// <copyright file="RoleAssignedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
/// Handles RoleAssigned domain events.
/// </summary>
public sealed class RoleAssignedHandler : INotificationHandler<RoleAssigned>
{
    private readonly ILogger<RoleAssignedHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleAssignedHandler"/> class.
    /// </summary>
    public RoleAssignedHandler(ILogger<RoleAssignedHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(RoleAssigned notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("RoleAssigned handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}

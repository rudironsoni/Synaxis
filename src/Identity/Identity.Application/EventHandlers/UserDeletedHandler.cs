// <copyright file="UserDeletedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
/// Handles UserDeleted domain events.
/// </summary>
public sealed class UserDeletedHandler : INotificationHandler<UserDeleted>
{
    private readonly ILogger<UserDeletedHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDeletedHandler"/> class.
    /// </summary>
    public UserDeletedHandler(ILogger<UserDeletedHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserDeleted notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("UserDeleted handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}

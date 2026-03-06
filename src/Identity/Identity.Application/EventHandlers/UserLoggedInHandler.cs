// <copyright file="UserLoggedInHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
/// Handles UserLoggedIn domain events.
/// </summary>
public sealed class UserLoggedInHandler : INotificationHandler<UserLoggedIn>
{
    private readonly ILogger<UserLoggedInHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserLoggedInHandler"/> class.
    /// </summary>
    public UserLoggedInHandler(ILogger<UserLoggedInHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserLoggedIn notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("UserLoggedIn handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}

// <copyright file="UserLoginFailedHandler.cs" company="Synaxis">
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
/// Handles UserLoginFailed domain events.
/// </summary>
public sealed class UserLoginFailedHandler : INotificationHandler<UserLoginFailed>
{
    private readonly ILogger<UserLoginFailedHandler> logger;

    /// <summary>
    </summary>
    <param name="logger">The logger instance.</param>
    <summary>
    /// Initializes a new instance of the <see cref="UserLoginFailedHandler"/> class.
    /// </summary>
    public UserLoginFailedHandler(ILogger<UserLoginFailedHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserLoginFailed notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("UserLoginFailed handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}

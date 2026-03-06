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
/// Handles UserLoginFailed domain events.
/// </summary>
public sealed class UserLoginFailedHandler : INotificationHandler<UserLoginFailed>
{
    private readonly ILogger<UserLoginFailedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserLoginFailedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public UserLoginFailedHandler(ILogger<UserLoginFailedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserLoginFailed notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("UserLoginFailed handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}

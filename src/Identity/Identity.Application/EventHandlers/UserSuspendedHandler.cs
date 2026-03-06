// <copyright file="UserSuspendedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
/// Handles UserSuspended domain events.
/// </summary>
public sealed class UserSuspendedHandler : INotificationHandler<UserSuspended>
{
    private readonly ILogger<UserSuspendedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSuspendedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public UserSuspendedHandler(ILogger<UserSuspendedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserSuspended notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("UserSuspended handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}
